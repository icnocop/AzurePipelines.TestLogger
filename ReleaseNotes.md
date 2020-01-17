# 1.0.4

- [Feature] Added support for using default credentials instead of an access token for authentication.

# 1.0.3

- [Fix] Updated Azure API endpoints to 5.0 final versions since previews are 404 now.

# 1.0.2

- [Fix] Another fix for test runs without any results

# 1.0.1

- [Fix] Makes sure not to end test runs that didn't send any results

# 1.0.0

- [Refactoring] Code quality improvements

# 0.4.7

- [Fix] Sends start dates when marking as completed so they don't get reset

# 0.4.6

- [Feature] Sets start dates

# 0.4.5

- [Fix] Fix for API version when patching a test run as completed
- [Feature] Sets completion dates

# 0.4.4

- [Fix] Fix for patching aggregate failure outcome

# 0.4.3

- [Fix] Fixes result grouping for heirarchy display in Pipelines UI

# 0.4.2

- [Feature] Sets test states and outcomes

# 0.4.1

- [Fix] Fixed a bug with source parsing and file paths
- [Fix] Fixed a bug with method arguments in test names

# 0.4.0

- [Feature] Now groups tests under their fixture

# 0.3.0

- [Refactoring] Renamed to AzurePipelines.TestLogger

# 0.2.2

- [Feature] Better test run names
- [Fix] Common API version for all endpoints

# 0.2.0

- [Fix] Big refactoring to create a new test run before posting results

# 0.1.3

- [Fix] Using basic auth instead of bearer token

# 0.1.0

- Initial release