# DBLint - _Find bugs in databases_

This project has been developed by aweis on [CodePlex](https://dblint.codeplex.com/).
Because CodePlex is [shooting down](https://blogs.msdn.microsoft.com/bharry/2017/03/31/shutting-down-codeplex/) very soon and I always liked this very useful application, I decided to migrate the source code to GitHub.


## Project Description
DBLint is an automated tool for analyzing database designs. DBLints ensures a consistent and maintainable database design by identifying bad design patterns.

## Abstract
Evaluating the quality and consistency of a database schema by a manual review is time-consuming and error-prone. To accommodate this challenge, we propose DBLint, a fast, configurable, and extensible tool for automated analysis of database design. DBLint currently includes 46 design rules derived from good database design practices. The rules discover design errors, which are collected as issues and presented in an interactive report. The issues are used to calculate a score for each table and an overall score. The scores are based on the severities of the issues, their location in the schema, and a table-importance measure. DBLint has been tested extensively on more than 35 real-world schemas, identifying a large number of relevant issues. Developers from four organizations have evaluated DBLint and found it to be useful and relevant, in particular the overall score and report.
