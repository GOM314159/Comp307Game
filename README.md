🧵 String Theory

String Theory is an immersive, first-person survival horror game built in Unity. Trapped in a twisted, dynamic theater, players must navigate escalating horrors over four days, outsmarting terrifying animatronic/doll enemies driven by complex behavior trees.

Will you survive the puppet show, or will they pull your strings?

🌟 Key Features

Immersive First-Person Controller: Features procedural headbob, dynamic crouching, and physical bone-tracking for an incredibly grounded, realistic survival horror camera experience (inspired by Outlast and Resident Evil 7).

Dynamic Environments: The world reacts to your actions. Cutting cables, solving cases, and progressing through the days will physically alter the level (e.g., transitioning the PuppetShow Theater into its destroyed state).

Advanced AI & Behavior Trees: Enemies like the Miniboss, Vashin, and the Minions are powered by the Unity Behavior package. They dynamically patrol, hunt, and react to environmental triggers (like falling chandeliers) in real-time.

Day & Case System: Survive through 4 escalating days of terror. Features a built-in Case Manager, objective tracking, and a high-stakes Hardcore Mode that resets all progress upon death.

Progression & Collectibles: Find items to survive (like the Cable Cutters), gather Exodia pieces, use the Gacha system, and earn permanent strength and speed modifiers.

Gender-Specific Rigs: Choose between male (Nephew) and female (Niece) characters, featuring dynamic animator assignments and rig-specific camera attachments.

🛠️ Technical Details

Engine: Built with Unity 6 (URP).

Input: Utilizes the Unity New Input System for smooth controller and keyboard/mouse support.

AI: Built using the experimental/new Unity Behavior Graph system for modular, parallel, and interruptible enemy logic.

Architecture: Heavily relies on Persistent Managers (GameStateManager, GameFlowManager) and event-driven architecture to maintain state across scenes and menu transitions.

🎮 Controls (Default)

W, A, S, D - Move

Mouse - Look

Left Shift - Sprint

C / Ctrl - Crouch

Space - Jump

E / Left Click - Interact (Pick up items, cut cables)

🚀 Getting Started for Developers

Prerequisites

Unity 6 (6000.0.x or higher)

Universal Render Pipeline (URP)

Unity Behavior Package

Input System Package

Installation

Clone the repository:

git clone https://github.com/YourUsername/String-Theory.git


Open Unity Hub and click Add.

Navigate to the cloned folder and select it.

Open the GamePlayTest or MainMenu scene located in Assets/Scenes/ to begin testing.

📁 Core Systems Overview

GameFlowManager.cs: The heart of the scene transitions. Handles moving the player from the Main Menu to the Lobby, managing death states, and applying Day 1-4 environmental changes.

CameraAttachment.cs: A custom, decoupled camera script that magnetically tracks the player's animated head bone for physical drops (crouching) while utilizing procedural sine-waves for nausea-free headbobbing.

GameStateManager.cs: A persistent singleton tracking everything from Gacha currency and Exodia pieces to current objectives and Hardcore mode status.

📜 License

This project is licensed under the MIT License - see the LICENSE file for details.
