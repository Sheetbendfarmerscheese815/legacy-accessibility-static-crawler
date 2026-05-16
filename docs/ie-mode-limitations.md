# IE Mode Limitations

Edge IE-mode-assisted workflows are intentionally conservative.

Known limitations:

- Selenium may not inspect every legacy control.
- ActiveX, object, embed, applet, and legacy document-mode content may be opaque.
- DOM capture can be incomplete.
- Keyboard behavior and focus visibility require manual review.
- Screen reader behavior must be tested manually.

When capture is limited or legacy risks are detected, the scanner adds a `Legacy IE Mode Manual Review Required` finding.
