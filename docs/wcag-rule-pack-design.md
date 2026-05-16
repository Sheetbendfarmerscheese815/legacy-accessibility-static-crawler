# WCAG Rule Pack Design

Rule packs are JSON files copied to output and embedded in the Core assembly.

Rules include:

- RuleId
- Standard
- SuccessCriterion
- Level
- Title
- Description
- AppliesTo
- TestMethod
- StaticCheckSupported
- ManualReviewRequired
- SeverityDefault
- Keywords
- RemediationGuidance
- References

Only deterministic checks are treated as static findings. Rules requiring judgment are represented as manual review requirements.
