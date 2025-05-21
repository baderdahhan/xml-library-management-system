# Git Workflow for XML Library Management System

## Repository Structure

This project uses Git for version control, providing the following benefits:
- Track all changes to the codebase
- Support collaborative development
- Enable feature branching and parallel development
- Provide rollback capability if issues are discovered

## Branch Strategy

The repository follows a feature branch workflow:

- `main` - The primary branch containing stable, production-ready code
- `feature/*` - Feature branches for new development
- `bugfix/*` - Branches for fixing issues
- `release/*` - Release preparation branches

## Workflow Instructions

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/xml-library-system.git
cd xml-library-system

# Make sure you're on the main branch
git checkout main

# Pull the latest changes
git pull origin main
```

### Feature Development

```bash
# Create a new feature branch
git checkout -b feature/xml-validation

# Make your changes
# Edit files...

# Add the changes
git add LibraryAPI/Services/XmlService.cs

# Commit with a descriptive message
git commit -m "Implement XML schema validation with custom error handling"

# Push to remote repository
git push origin feature/xml-validation
```

### Code Review Process

1. Create a pull request from your feature branch to main
2. Assign reviewers to check your code
3. Address any feedback from the review
4. Once approved, merge into main

### Merging Back to Main

```bash
# Switch to main branch
git checkout main

# Pull the latest changes
git pull origin main

# Merge the feature branch
git merge feature/xml-validation

# Push the updated main branch
git push origin main
```

### Handling Conflicts

If you encounter merge conflicts:

```bash
# During a merge with conflicts
git status  # Check which files have conflicts

# Edit the conflicted files to resolve issues

# After resolving
git add <resolved-files>
git commit -m "Resolve merge conflicts from feature/xml-validation"
```

## Commit Message Guidelines

Follow these guidelines for commit messages:

- Use the imperative mood ("Add feature" not "Added feature")
- Keep the first line under 50 characters
- Provide more detailed explanation in the body if needed
- Reference issue numbers if applicable

Examples:
```
Implement XSD validation for book records
Add JWT token authentication
Fix validation error handling in XML service
Update XSLT transformation for reports
```

## Ignored Files

The `.gitignore` file is configured to exclude:
- Build artifacts (bin, obj folders)
- IDE-specific files (.vs, .vscode)
- User-specific settings
- NuGet packages

## Best Practices

1. Commit frequently with small, focused changes
2. Pull latest changes before starting new work
3. Keep feature branches short-lived
4. Write meaningful commit messages
5. Always test before committing
6. Don't commit sensitive information (API keys, passwords)

## Git Commands Reference

- `git status` - Check the current state of your working directory
- `git log` - View commit history
- `git branch` - List all branches
- `git checkout -b <branch-name>` - Create and switch to a new branch
- `git push origin <branch-name>` - Push changes to remote branch
- `git pull` - Get the latest changes from remote
- `git merge <branch-name>` - Merge changes from a branch 