#!/usr/bin/env python3
"""
extract-bd-summary.py — BD.md → bd-summary.json

Extracts a structured JSON summary from a BD.md file.
The summary is used by the generate workflow as a low-token planning aid.
This script is best-effort: it outputs partial results with warnings on parse failures.

Usage:
    python3 extract-bd-summary.py --input <BD.md path> --output <bd-summary.json path>
"""

import argparse
import json
import os
import re
import sys


# ---------------------------------------------------------------------------
# Global settings loader
# ---------------------------------------------------------------------------

_SETTINGS_PATH = os.path.join(os.path.dirname(__file__), "..", "settings.yaml")

def _load_settings():
    """Load skill-level settings.yaml. Prints warning and returns empty dict on failure."""
    try:
        import yaml
    except ImportError:
        print("WARNING: pyyaml not installed — settings.yaml not loaded, using empty defaults.", file=sys.stderr)
        return {}
    if not os.path.exists(_SETTINGS_PATH):
        print(f"WARNING: settings.yaml not found at {_SETTINGS_PATH} — using empty defaults.", file=sys.stderr)
        return {}
    with open(_SETTINGS_PATH, "r", encoding="utf-8") as f:
        return yaml.safe_load(f) or {}

_SETTINGS = _load_settings()


def _get(path, default=None):
    """Get a nested value from settings by dot-separated path."""
    node = _SETTINGS
    for key in path.split("."):
        if isinstance(node, dict):
            node = node.get(key)
        else:
            return default
    return node if node is not None else default


# ---------------------------------------------------------------------------
# Field type / validation heuristics (loaded from settings.yaml)
# ---------------------------------------------------------------------------

FIELD_TYPE_KEYWORDS = _get("bd_analysis.field_type_keywords", {})
VALIDATION_KEYWORDS = _get("bd_analysis.validation_keywords", {})
NAVIGATION_KEYWORDS = _get("bd_analysis.navigation_keywords", [])

FIELD_LABEL_MAX_LENGTH = _get("bd_analysis.field_label_max_length", 40)
KEY_VALUE_SECTION_THRESHOLD = _get("bd_analysis.key_value_section_threshold", 0.6)
SECTION_SUMMARY_MAX_CHARS = _get("bd_analysis.section_summary_max_chars", 120)
FIELD_NAME_TRUNCATE_LENGTH = _get("bd_analysis.field_name_truncate_length", 60)


def detect_field_type(text: str) -> str:
    text_lower = text.lower()
    for ftype, keywords in FIELD_TYPE_KEYWORDS.items():
        for kw in keywords:
            if kw.lower() in text_lower:
                return ftype
    return "unknown"


def detect_validation(text: str) -> str:
    text_lower = text.lower()
    for vtype, keywords in VALIDATION_KEYWORDS.items():
        for kw in keywords:
            if kw.lower() in text_lower:
                return vtype
    return "unknown"


def is_navigation_line(text: str) -> bool:
    text_lower = text.lower()
    return any(kw.lower() in text_lower for kw in NAVIGATION_KEYWORDS)


# ---------------------------------------------------------------------------
# Parsing helpers
# ---------------------------------------------------------------------------

def detect_section_type(lines: list[str]) -> str:
    """Classify a section's content type."""
    has_table = any(line.strip().startswith("|") for line in lines)
    kv_count = sum(1 for line in lines if ":" in line and not line.strip().startswith("#"))
    if has_table:
        return "data_table"
    if kv_count > 2:
        return "key_value"
    return "plain"


def make_summary(lines: list[str], max_chars: int = SECTION_SUMMARY_MAX_CHARS) -> str:
    """Produce a brief text summary of a section's first non-empty lines."""
    parts = []
    for line in lines:
        stripped = line.strip()
        if stripped and not stripped.startswith("#") and not stripped.startswith("|") and not stripped.startswith("---"):
            parts.append(stripped)
        if sum(len(p) for p in parts) >= max_chars:
            break
    text = " ".join(parts)
    return text[:max_chars] + ("…" if len(text) > max_chars else "")


# ---------------------------------------------------------------------------
# Main parser
# ---------------------------------------------------------------------------

def parse_bd(path: str) -> dict:
    warnings: list[str] = []

    with open(path, encoding="utf-8") as fh:
        raw = fh.read()

    all_lines = raw.splitlines()
    total_lines = len(all_lines)

    # ---- Split into top-level "sheet" blocks delimited by `---` or `# ` headings ----
    sheets: list[dict] = []
    fields: list[dict] = []
    navigation_targets: list[dict] = []
    validation_rules: list[dict] = []

    # Detect sheet boundaries: lines that are `---` separators or H1 headings
    sheet_starts: list[tuple[int, str]] = []
    for i, line in enumerate(all_lines):
        if re.match(r"^# .+", line):
            sheet_starts.append((i, line.lstrip("# ").strip()))
        elif line.strip() == "---" and i > 0:
            # Use preceding heading if available, else "Sheet {n}"
            prev_heading = next(
                (all_lines[j].lstrip("# ").strip() for j in range(i - 1, max(i - 5, -1), -1)
                 if re.match(r"^#+ .+", all_lines[j])),
                f"Sheet {len(sheet_starts) + 1}",
            )
            sheet_starts.append((i + 1, prev_heading))

    # Deduplicate and sort
    seen = set()
    sheet_starts_clean: list[tuple[int, str]] = []
    for idx, name in sorted(sheet_starts, key=lambda x: x[0]):
        if idx not in seen:
            seen.add(idx)
            sheet_starts_clean.append((idx, name))

    if not sheet_starts_clean:
        # Treat entire file as one sheet
        sheet_starts_clean = [(0, os.path.basename(path))]

    # Build sheet ranges
    for si, (start, sheet_name) in enumerate(sheet_starts_clean):
        end = sheet_starts_clean[si + 1][0] - 1 if si + 1 < len(sheet_starts_clean) else total_lines
        sheet_lines = all_lines[start:end]

        # ---- Parse sections within sheet (H2/H3 headings) ----
        sections: list[dict] = []
        sec_starts: list[tuple[int, str]] = []
        for j, line in enumerate(sheet_lines):
            m = re.match(r"^(#{2,3}) (.+)", line)
            if m:
                sec_starts.append((j, m.group(2).strip()))

        for ssi, (sec_start_rel, heading) in enumerate(sec_starts):
            sec_end_rel = sec_starts[ssi + 1][0] if ssi + 1 < len(sec_starts) else len(sheet_lines)
            sec_lines = sheet_lines[sec_start_rel:sec_end_rel]
            abs_start = start + sec_start_rel
            abs_end = start + sec_end_rel - 1
            sec_type = detect_section_type(sec_lines)
            sec_summary = make_summary(sec_lines[1:])

            sections.append({
                "heading": heading,
                "line_range": [abs_start + 1, abs_end + 1],  # 1-based
                "type": sec_type,
                "summary": sec_summary,
            })

            # ---- Extract fields from key-value or table rows ----
            for k, sec_line in enumerate(sec_lines):
                abs_line = abs_start + k + 1  # 1-based

                # Key-value pattern: "Label: value" or "**Label**: value"
                kv_match = re.match(rf"^\*{{0,2}}([^|*\n:]{{2,{FIELD_LABEL_MAX_LENGTH}}})\*{{0,2}}\s*:\s*(.+)", sec_line)
                if kv_match:
                    fname = kv_match.group(1).strip()
                    fval = kv_match.group(2).strip()
                    ftype = detect_field_type(fval + " " + fname)
                    fval_type = detect_validation(fval + " " + fname)

                    if ftype != "unknown" or fval_type != "unknown":
                        fields.append({
                            "name": fname,
                            "type": ftype,
                            "validation": fval_type,
                            "sheet": sheet_name,
                            "line": abs_line,
                        })

                    # Validation rules
                    if fval_type in ("required", "format", "maxlength"):
                        # Try to find error message in nearby lines
                        error_msg = ""
                        for nearby in sec_lines[max(0, k - 2):k + 3]:
                            msg_m = re.search(r"[「「](.*?)[」」]", nearby)
                            if msg_m:
                                error_msg = msg_m.group(1)
                                break
                        validation_rules.append({
                            "field": fname,
                            "rule": fval.strip()[:120],
                            "error_message": error_msg,
                            "line": abs_line,
                        })

                # Table rows (Markdown table)
                if sec_line.strip().startswith("|") and not sec_line.strip().startswith("|---"):
                    cells = [c.strip() for c in sec_line.strip().strip("|").split("|")]
                    for cell in cells:
                        cell_ftype = detect_field_type(cell)
                        cell_vtype = detect_validation(cell)
                        if cell_ftype != "unknown" or cell_vtype in ("required", "format", "maxlength"):
                            fields.append({
                                "name": cell[:FIELD_NAME_TRUNCATE_LENGTH],
                                "type": cell_ftype,
                                "validation": cell_vtype,
                                "sheet": sheet_name,
                                "line": abs_line,
                            })

                # Navigation targets
                if is_navigation_line(sec_line):
                    # Try to extract button/link label and destination
                    nav_m = re.search(
                        r"[「「](.*?)[」」]|「(.*?)」|(button|link|ボタン|リンク)[^\S\r\n]+([^\n,、。]+)",
                        sec_line,
                        re.IGNORECASE,
                    )
                    label = nav_m.group(0).strip("「」") if nav_m else sec_line.strip()[:60]
                    # Destination: rest of line after arrow or keyword
                    dest_m = re.search(r"(?:→|遷移先|画面|screen)[^\S\r\n]*[：:：]?\s*(.+)", sec_line)
                    destination = dest_m.group(1).strip()[:80] if dest_m else ""
                    navigation_targets.append({
                        "label": label[:80],
                        "destination": destination,
                        "line": abs_line,
                    })

        sheets.append({
            "name": sheet_name,
            "line_range": [start + 1, end],
            "sections": sections,
        })

    # Deduplicate fields (same name+sheet+line)
    seen_fields: set[tuple] = set()
    unique_fields: list[dict] = []
    for f in fields:
        key = (f["name"], f["sheet"], f["line"])
        if key not in seen_fields:
            seen_fields.add(key)
            unique_fields.append(f)

    # Deduplicate navigation targets
    seen_nav: set[int] = set()
    unique_nav: list[dict] = []
    for n in navigation_targets:
        if n["line"] not in seen_nav:
            seen_nav.add(n["line"])
            unique_nav.append(n)

    if not sheets:
        warnings.append("No sheets detected — BD.md may be empty or non-standard.")

    result = {
        "source_file": os.path.basename(path),
        "total_lines": total_lines,
        "sheets": sheets,
        "fields": unique_fields,
        "navigation_targets": unique_nav,
        "validation_rules": validation_rules,
        "statistics": {
            "total_sheets": len(sheets),
            "total_sections": sum(len(s["sections"]) for s in sheets),
            "total_fields": len(unique_fields),
            "total_navigation_targets": len(unique_nav),
            "total_validation_rules": len(validation_rules),
        },
    }

    if warnings:
        result["warnings"] = warnings

    return result


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Extract a structured JSON summary from BD.md for token-efficient planning."
    )
    parser.add_argument("--input", required=True, help="Path to the BD.md source file")
    parser.add_argument("--output", required=True, help="Path for the output bd-summary.json")
    args = parser.parse_args()

    if not os.path.isfile(args.input):
        print(f"ERROR: Input file not found: {args.input}", file=sys.stderr)
        sys.exit(1)

    try:
        summary = parse_bd(args.input)
    except Exception as exc:  # pylint: disable=broad-except
        print(f"WARNING: Partial parse failure — {exc}", file=sys.stderr)
        summary = {
            "source_file": os.path.basename(args.input),
            "total_lines": 0,
            "sheets": [],
            "fields": [],
            "navigation_targets": [],
            "validation_rules": [],
            "statistics": {
                "total_sheets": 0,
                "total_sections": 0,
                "total_fields": 0,
                "total_navigation_targets": 0,
                "total_validation_rules": 0,
            },
            "warnings": [f"Parse failure: {exc}"],
        }

    os.makedirs(os.path.dirname(os.path.abspath(args.output)), exist_ok=True)
    with open(args.output, "w", encoding="utf-8") as fh:
        json.dump(summary, fh, ensure_ascii=False, indent=2)

    stats = summary.get("statistics", {})
    print(
        f"BD summary written to: {args.output}\n"
        f"  Sheets: {stats.get('total_sheets', 0)} | "
        f"Sections: {stats.get('total_sections', 0)} | "
        f"Fields: {stats.get('total_fields', 0)} | "
        f"Nav targets: {stats.get('total_navigation_targets', 0)} | "
        f"Validation rules: {stats.get('total_validation_rules', 0)}"
    )
    if summary.get("warnings"):
        for w in summary["warnings"]:
            print(f"  WARNING: {w}", file=sys.stderr)


if __name__ == "__main__":
    main()
