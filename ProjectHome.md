MergeSvnRepos merges multiple subversion repositories into a single, new repository.

The tool maintains the correct flow of revision dates, though it will mangle all existing revision numbers (i.e., no existing revisions will have the same number in the new repository). It maintains correct history, modifying revision numbers as it goes.