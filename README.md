# AppSentinel Service Launcher Template
A professional C#/.NET reference implementation for orchestrating processes from a Windows Service while bypassing Session 0 Isolation. This template provides a strategic framework to launch interactive GUI applications and high-privilege background tasks with precise control over security tokens and environment contexts.

🚀 Core Launch Modes
This project solves the "interactivity vs. privilege" trade-off by offering three distinct execution modes:

User Mode:
Identity: Runs as the currently logged-on interactive user.
Key Advantage: Perfectly resolves user-specific environment variables and shell paths (e.g., Desktop, Documents).
Best For: GUI tools, OpenFileDialog interactions, and user-profile dependent tasks.

Admin Mode:
Identity: Runs as SYSTEM via winlogon.exe token duplication.
Key Advantage: Bypasses UAC prompts while running in the user's interactive session.
Best For: Legacy elevation tasks or scenarios where standard admin rights are sufficient.

System Mode:
Identity: Runs as LocalSystem using the Service's own primary token with session redirection.
Key Advantage: Retains full SYSTEM privileges (e.g., SeTimeZonePrivilege) that are often stripped in winlogon tokens.
Best For: Core background agents, system-level configuration, and hardware-near operations.

🛠️ Technical Highlights
Token Manipulation: Advanced use of DuplicateTokenEx and SetTokenInformation to redirect system tokens into active user sessions.
Environment Orchestration: Proper use of CreateEnvironmentBlock to ensure the target process inherits the correct user profile context.
Interactive Desktop Access: Explicit window station mapping to winsta0\default for GUI visibility across session boundaries.
Foreground Lock Bypass: Implementation of AllowSetForegroundWindow to ensure launched UI components gain focus correctly.
