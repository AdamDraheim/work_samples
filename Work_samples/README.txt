Work Samples Descriptions
-------------------------
QuestText_Cpp_files contains the set up of part of the backend of a program
I am working on that creates a text based quest sheet. This quest sheet
is then used in my main current project, Bygone, which reads a text file
and creates a quest based on the text file in Unity. The purpose of this
program is to more efficiently create these quest documents. This project
is in C++.

NodeMapManager.cs was made for my main current project, and it generates, for
the current level, a map of all current walkable locations, as well as
provides functionality for calculating the fastest route there.

PhysicsBody2D.cs is the main physics file for my current capstone game. I am
in charge of developing the physics for my game, and this regulates gravity,
rotation, velocities, and forces as well as the linear algebra calculations between
them.

QuestHandler.cs is the Unity-side aspect of my quest system. It tracks
current quests the player is doing as well as saves and loads them. It
uses a quest parser to read text files and makes them into quests. This
acts as the intermediate between quests and the world.

WorldManager.cs handles data persistence for my current main project. The idea
is modeled after Bethesda games handling of npc persistence, where leaving
an area does not mean the enemy resets, but rather they are still there
waiting for the player for a set amount of time. Thus, it was necessary for
the data to be saved between levels and to know how to check for NPC's
as well as when to put new ones down. This file handles all that and saving
NPC instances.

WorldMesh.cs was for a jam project where I wanted to experiment with
procedural generation on the Unity mesh system. This used a variety of
algorithms to shape the world including perlin noise and a Bezier curve
for river generation.