AITestMaker -- AI‑powered test generator for Windows · DAM Final Project 2025/2026

AITestMaker is a Windows desktop application that creates multiple‑choice tests automatically using different AI models. You enter a topic, choose a difficulty level, and the app generates a complete test with questions and answers. It also stores your test history and allows exporting results to PDF.

*Main Features

-- Generate tests from any topic

-- Difficulty levels: Easy, Medium, Hard

-- Works with DeepSeek, Groq, Gemini and LM Studio

-- Local history stored in SQLite

-- PDF export

-- Secure login system and guest mode

-- WPF interface using MVVM

*Installation
Requirements: Windows 10+, Internet connection, 200 MB free space, .NET 8 Runtime.

Steps:

1- Download the installer from the Releases section.

2- Run the .exe or .msi.

3- Accept the license and choose the installation folder.

4- Launch the app from the desktop shortcut.

*How It Works

-- Login
Log in with your account, create a new one, or use guest mode (one test per session).

-- Main Screen
Choose difficulty, enter a topic, select the AI model, review your topic history, and generate the test.

-- Test Screen
Timed test depending on difficulty. Each question has four options. A sidebar lets you navigate between questions. At the end, you can view your score and summary.

-- Test History
Shows all completed tests with date, topic, difficulty, number of questions, score, and PDF export.

-- Data Model
SQLite database with Users, Test, Question and Option tables, following a 1:N structure.

-- Development Challenges
Handled invalid AI JSON, improved UI responsiveness with async operations, strengthened authentication, and redesigned the interface for clarity.

-- Future Improvements
Question bank, document chunking, new question types, automatic consistency checking, and a future Android version using MAUI.

Author
Rafael (belo-dev)
DAM Final Project
I.E.S. Guadalpeña · 2025/2026
