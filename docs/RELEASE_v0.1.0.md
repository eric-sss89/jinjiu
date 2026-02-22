# RELEASE v0.1.0

Jinjiu v0.1.0 delivers a full MVP loop:

`Capture -> Orchestrator -> (optional DecisionAgent) -> InputDriver`

## What is ready
- End-to-end pipeline is runnable on Windows
- Basic scene-driven action generation works
- Agent-mode decisions can override rule mode with safe fallback
- Safety controls are in place for guarded operation

## What is not included yet
- Real input injection (currently simulation mode)
- OCR and advanced vision model integration
- Full desktop GUI control panel

## Recommended next step
- v0.2.0: add real input injection behind strict guardrails and richer perception (OCR + templates)
