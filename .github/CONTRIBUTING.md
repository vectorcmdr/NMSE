# Contributing to NMSE

Thank you for your interest in contributing! This project is licensed under **AGPL-3.0**, and we welcome contributions of all kinds.

## 👋 Getting Started

1. Fork the repository and clone it locally.
2. Create a branch named `feature/your-feature` or `fix/your-bug`.
3. Run existing tests and make sure everything passes.

```bash
# dotnet test
```

## 📌 Code Style

- Follow existing project / code conventions.
- Favour small, efficient functions over bloated methods where applicable.
- Reusable utility code should be in utility classes.
- Keep changes small and focused.
- Include tests for new behavior where appropriate.
- Do not introduce external packages and libraries.
- Do not include any code that is not compatible with the license.
- Do not include any agent instructions, skills, etc. if the code used LLM assistance.
- Any UI strings must pass through the localisation layer.

## 📝 Pull Requests

- Use the provided PR template.
- Link related issues and provide a clear description.
- Ensure the PR builds on CI and all tests pass.
- LLM assisted code with human scoping and review is acceptable but not preferable.
- Massive PRs with little to no description will be rejected.

## 🧰 Issues

- Search existing issues before opening a new one.
- Use the issue templates for bugs and feature requests.
- General questions are discouraged.

## 📚 Documentation

If your change affects public behavior, update docs in `docs/dev/` and any relevant README files.

## ❤️ Code of Conduct

By contributing, you agree to abide by our [Code of Conduct](CODE_OF_Conduct.md).

## 💰 Sponsorship & Donations

If you’d like to support ongoing development, consider sponsoring the project via [GitHub Sponsors](https://github.com/sponsors/vectorcmdr) or donating via a [Ko-fi](https://ko-fi.com/vector_cmdr). Supporting the project helps cover hosting, tooling, and maintenance time.

  <a href="https://github.com/sponsors/vectorcmdr" target="_blank" rel="noopener noreferrer">
    <img src="https://img.shields.io/badge/GitHub-Sponsors-000000?logo=github&logoColor=ffffff" alt="GitHub Sponsors" />
  </a>

  <a href="https://ko-fi.com/vector_cmdr" target="_blank" rel="noopener noreferrer">
    <img src="https://img.shields.io/badge/Ko--fi-Donate-29abe0?logo=ko-fi&logoColor=ffffff" alt="Ko-fi" />
  </a>
