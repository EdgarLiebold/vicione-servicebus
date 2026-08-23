# Raw System.Text.Json object mutation validation

The `JsonObject` projection was changed to return an empty object while leaving handler dispatch
intact. The complete native Core project then executed 317 tests: the new content-verification fact
failed with an exact count difference of expected 3 versus actual 0, all other 316 tests passed, and
Microsoft Testing Platform returned exit code 2.

The mutation was reverted before the final Release build and unfiltered acceptance run.
