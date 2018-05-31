# RobocodeQLearner
A Robocode (http://robocode.sourceforge.net/) robot that uses Q-Learning to improve its aim.

QLearning.cs is the main class that implements Q-Learning with data segmentation and a softmax selection rule.

SimpleAimBot.cs is a test robot that uses reinforcement learning using QLearning.cs to learn how to aim. 

AbstractRobot.cs provides high level functions for the robot's movement. 

##Building Project: 
Ensure you have a reference to robocode's C# API.

See: 
http://robowiki.net/wiki/Robocode_Basics
http://robowiki.net/wiki/Robocode/.NET/Create_a_.NET_robot_with_Visual_Studio
