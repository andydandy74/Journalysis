# AGENTS.md

## Role & Persona
- **Role:** Coding Assistant
- **Current Focus:** Writing unit tests ONLY.
- **Workflow Restriction:** Do not attempt to run or execute tests in the shell/terminal. The user will execute and run tests themselves in Visual Studio. Focus entirely on designing, implementing, and writing robust unit tests and test fixtures for the codebase.
- **Source Changes:** Parser/source fixes are made only on explicit user request (the default scope remains writing tests). When a bug blocks testing, point it out first and ask before editing source code.

## Step-by-Step Workflow for Writing Unit Tests
1. **Target Specification:** The user specifies what we want to write tests for.
2. **Code Identification & Confirmation:** The assistant locates the relevant source code in `Journalysis.Core` and confirms with the user that this is correct.
3. **Sample Collection:** The user provides real sample journal lines (a complete line plus any cut-off/truncated variants) **before** the assistant proposes any scenarios. Do not imagine or propose test cases without samples; the supplied lines are the required basis for scenario derivation.
4. **Scenario Analysis & Confirmation:** The assistant examines the code **thoroughly** – including helper methods (e.g. `ParseBasicFileInfoAttributes`, `ParseCommandType`) and all `FormatException` throw sites – then derives candidate scenarios from the provided samples. Collapse samples whose observable outcome is identical (set properties plus exception name) into a single case, and explicitly point out any throw sites or behaviors the samples do not cover so the user can decide whether to add cases. Confirm the derived scenario list with the user.
5. **Test Data Creation:** The assistant creates the file-based test data (`.input.txt` and `.expected.json`) based on the samples, using descriptive non-numbered names and minimal per-case expected JSON (see JSON Property Convention).
6. **Review & Adjustment:** The user reviews and may adjust the test data.
7. **Test Implementation:** The assistant writes the actual unit test implementation.
8. **Execution & Refinement:** The user runs the tests in Visual Studio and requests refinements if needed.

## Practical Learnings & Rules of Thumb
- **Pass 1 (`JournalRecordBuilder`) Prerequisites:** Test input lines (`.input.txt`) must satisfy Pass 1 classification rules (e.g. exact prefixes like `":< API_SUCCESS { "` or `' [Jrn.BasicFileInfo]`) so that the record is correctly instantiated as the target `JournalLine` subtype before reaching Pass 2. Otherwise `journal.Lines` / `journal.ParsingErrors` stay empty and tests fail with `Assert.Single()`.
- **Parsing Errors & Exceptions:** Test cases expecting a `FormatException` and an entry in `journal.ParsingErrors` require the source line to successfully pass Pass 1 classification, ensuring it is present in the journal records collection for subsequent Second-Pass validation.
- **Samples are not Test Cases:** Always request journal lines from the user before proposing scenarios. Supplied lines are examples/starting points, NOT ready-made test cases. Do not turn each sample into its own test case; derive the scenario-based cases from the samples and the agreed scenario list, collapsing variants with identical outcomes.
- **Exception Timing:** For expected `FormatException`, determine where in the parser the exception is thrown and which properties are already set at that point. Only assert those properties (plus `"Exception": "FormatException"`) in the `.expected.json`; leave unset properties out so the test does not require them to be null.
- **JSON Property Convention:** Only properties explicitly present in `.expected.json` are asserted. Each `.expected.json` contains **only** the values that are the actual subject of that specific test case — never values that are merely "not set" or incidental defaults (e.g. do NOT write `"Machine": null` in a Build/Branch/Release case). Write `null` only when the absence of the value is itself the expected, relevant outcome (e.g. `"Release": null` as the cause of a `FormatException`, or `"Path": null` for an empty path). Include `CommentLineCount` / `RawTexts` only when the record count or the merged raw text is what the case verifies (e.g. Pass 1 merging of the three build lines); for a plain comment `RawTexts` alone is sufficient.
- **Test-Data Naming:** Use descriptive kebab-case file names **without numeric prefixes** (`release-keyword-cut.input.txt`, not `07-release-keyword-cut.input.txt`). Do not number test data to impose order.
- **No Redundant Cases:** Add a case only when it verifies a distinct behavior. Drop cases whose outcome is already covered by another case (e.g. a truncated second machine line that is ignored because of `MachineNameFound`). Treat "outcome" as the asserted properties plus exception name.
- **Coverage Gaps:** After deriving scenarios from the samples, review every `FormatException` throw site and helper branch and explicitly flag any behavior the samples do not exercise. Let the user decide whether to add cases; do not invent speculative cases unprompted.
- **Deleting Files:** Shell commands are policy-blocked. When test data files need to be removed, ask the user to delete them manually.
