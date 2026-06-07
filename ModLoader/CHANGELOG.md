# Changelog

All notable changes to ModLoader will be documented in this file.

## [1.0.0] - 2025-12-03

### Added
- Initial release of ModLoader
- Core mod loading system with automatic DLL discovery
- `IMod` interface for mod implementation
- `ModBase` abstract class with built-in Harmony and logging
- `ModLogger` with console and file logging
- `ModManager` for loading and managing mods
- `UIHelper` with UI utility methods
- `GameHelper` with game system utilities
- Automatic Harmony patch application
- Error isolation per mod
- Comprehensive documentation:
  - README_MODDERS.md - Complete modding guide
  - INTEGRATION.md - Game integration guide
  - README.md - Project overview
- Example mod with UI modifications
- Automatic folder structure creation (Mods/, Logs/, Config/)

### Features
- ✅ Automatic mod loading from Mods/ folder
- ✅ Per-mod logging to separate files
- ✅ Harmony integration for runtime patching
- ✅ Helper classes for easy development
- ✅ Error handling and mod isolation
- ✅ Support for multiple mods in one DLL
- ✅ Mod discovery and querying system

### Developer Experience
- Clean API for mod creation
- Extensive documentation with examples
- Template project for quick start
- Helper methods for common tasks
- Detailed error messages and logging

## Future Versions

### Planned for 1.1.0
- [ ] Mod configuration system
- [ ] Mod dependency resolution
- [ ] Mod load order control
- [ ] Hot-reload support for development
- [ ] Event system for common game events
- [ ] Resource loading helpers
- [ ] Network sync utilities

### Planned for 1.2.0
- [ ] GUI mod manager in-game
- [ ] Mod enable/disable without restart
- [ ] Mod update checker
- [ ] Conflict detection between mods
- [ ] Performance monitoring per mod

### Ideas for Future
- Mod marketplace integration
- Visual mod creator/editor
- Mod sandboxing for security
- Mod analytics and telemetry (opt-in)
- Cross-game mod compatibility layer

## Version History

### Version Numbering
ModLoader follows semantic versioning (MAJOR.MINOR.PATCH):
- MAJOR: Breaking API changes
- MINOR: New features, backwards compatible
- PATCH: Bug fixes, backwards compatible

---

**Current Version: 1.0.0**

