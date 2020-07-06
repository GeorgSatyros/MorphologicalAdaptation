# MorphologicalAdaptation
 A repository holding a frozen clone of the Morphological Adaptation repo for the purposes of my Msc Thesis documentation.

## Showcase
https://youtu.be/4T0IYWywRuc

## Features
 - The four levels showcased and utilized to extract the results documented in the Thesis.
 - A Simulation Manager singleton in each environment that allows full control over the parameters of the simulation.
 - An extension of the LevelManager script that allows control over environmental parameters, Curriculum Learning and the fitness function.
 - A Sun GameObject addon to extend the heat levels of each environment, as discussed in the Thesis.
 - The three types of starting agents used in the showcase and/or Thesis: Snake, Turtle and Basic.
 
### Simulation Manager
 A singleton in each scene that allows full control over the simulation and its parameters.
 The most notable features are:
  - Maximum Timescale: The hard upper limit to how many times the normal speed the simulation is allowed to run.
  - Target Framerate: The manager will try to keep the framerate around this number.
  - Epoch Duration: How many seconds each epoch(generation) will be simulated for, before Selection happens.
  - Epochs: How many generations to simulate.
  - Data Recording Interval: Every how many seconds to record data for the current state of the simulation.
  - Simulation Mode: Keep to Optimized, unless showcasing results.
  - Initial Population: How many agents per generation.
  - Starting Mutation Chance: Keep around 0.01 to 0.1
  - Creature Prefab: The starting agent. The first generation is made by clones of this agent.
  - Prune Agents: Discard clearly unfit agents at half-time of each epoch.
  - Speciation Distance: -1 disables speciation, positive integers enable it with said distance, as discussed in the Thesis
  - Species List: Procides an overview of the current species at runtime
  - Use Agent Minimization: Apply Dimensionality Minimization
  - Target/Starting Edibles: How many edibles to spawn. Set both to the same positive integer to disable the effects of curriculum learning on edibles.
  - Use Best Agent: Enables Elitism.
  - Maximum Repeats: How many times to repeat the same experiment, as specified with the parameters above. Used to extract robust data.

## Use-case
 - Open an environment scene. Environment scenes can be found under '/Assets/Scenes/'.
 - Find Simulation Manager in the scene and tune any parameters, as specified above. 
 - Find the extension of LevelManager (i.e. CeilingLevelManager) and tune the environmental parameters. Enables Curriculum Learning.
 - Press Play.
 - After the simulation is finished, or after you stop the simulation, go to 'Assets/Data/'. Useful metrics, agent snapshots and sceenshots pertaining to the last simulation run can be found there.
