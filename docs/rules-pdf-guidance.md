# Rules PDF Guidance

User-supplied PDFs are optional guidance overlays.

The PDF loader:

- Extracts raw text with PdfPig.
- Splits content by page and likely headings or numbered sections.
- Attempts to identify WCAG, Section 508, ADA, severity, test method, and remediation text.
- Writes raw text, chunks, and normalized JSON.
- Marks PDF-derived rules as manual review by default.

PDF-derived rules enrich mapping, descriptions, severity, and remediation guidance. They do not replace built-in deterministic static rule packs.
