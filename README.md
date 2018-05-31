# RobocodeQLearner
A Robocode (http://robocode.sourceforge.net/) robot that uses Q-Learning to improve its aim.

QLearning.cs is the main class that implements Q-Learning with data segmentation and a softmax selection rule.

SimpleAimBot.cs is a test robot that uses reinforcement learning using QLearning.cs to learn how to aim. 

AbstractRobot.cs provides high level functions for the robot's movement. 