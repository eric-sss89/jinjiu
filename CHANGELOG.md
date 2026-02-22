# Changelog

## v0.2.0-next (in progress)

### Added
- InputDriver real-input skeleton (default OFF)
- Dual safety gate for real input: `driver.enabled` + `driver.unsafe.enabled`
- Initial key mapping for core actions (`1`, `Tab`, `5`, `W`)
- v0.2 next-step planning doc

## v0.1.0 - MVP runnable loop

### Added
- Screen capture module (`Jinjiu.Capture`)
- Orchestrator with ROI-based lightweight perception and FSM stabilization
- Input driver (simulation) with rate limit + dedup + safety guards
- ROI calibrator tool and ROI tuning docs
- DecisionAgent v1 and agent-mode integration
- Foreground-window focus guard via Win32 API
- Decision history logging for replay/debug
- Startup scripts (`start-mvp.ps1`, `start-mvp-agent.ps1`)
- Utility scripts (`stop-mvp.ps1`, `clean-outbox.ps1`)

### Safety
- `driver.enabled` gate
- `driver.stop` emergency brake
- Action whitelist
- Focus guard

### Docs
- MVP flow / acceptance / 5-min runbook
- State schema and AI decision interface v1
