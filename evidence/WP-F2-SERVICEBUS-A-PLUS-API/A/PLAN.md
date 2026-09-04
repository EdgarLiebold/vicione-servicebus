# Work package A execution plan

| Requirement | Implementation and evidence |
|---|---|
| Central build policy | Update root and nested MSBuild properties; compare evaluated behavior with the journal policy. |
| Enforced editor policy | Replace the root editor policy and add the required using-placement rule. |
| Repository formatting | Run formatter style and analyzer passes solution-wide, then verify without changes. |
| Nullable staging | Remove source disables, enable projects where clean, and narrowly annotate temporary package-B exceptions. |
| Warning closure | Build the three required graphs and fix each warning at its cause. |
| Comment hygiene | Review every candidate comment and run the exact assignment scan. |
| Working-state relocation | Move every tracked `.testagent` file to `evidence/native-tests/obligation-maps`. |
| TODO cleanup | Remove completed cache-consolidation work and retain only bounded open engineering items. |
| Documentation consistency | Update commands and minimum counts only; defer product-documentation rewriting to package G. |
| Test confidence | Run three identical unfiltered Release unit profiles and record their complete summaries. |

Public signatures and existing test intent remain unchanged. Mechanical nullable work exposed two
bounded runtime defects; both are corrected under the related-defect rule, protected by focused
regression tests, and recorded in the program deviations.
