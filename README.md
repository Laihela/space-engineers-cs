### This repository consists of my C# code for the programmable block in the game Space Engineers. Most files are complete, standalone scripts that work out-of-the-box when pasted into the block. Most scripts dependend on certain blocks being present on the ship, care has been taken to ensure proper error handling in case something is missing. Some scripts are only aware of blocks which are grouped together with the programmable block in the terminal, this allows multiple separate scripts to run on the same ship independently, such as industrial robot arms or turrets.

##### I have demonstrated some of these scripts on my YouTube channel:
* KillVehicle.cs: https://youtu.be/77TGSNw7zwE
* DefenseSystem.cs: https://youtu.be/7nCJecg-6HU
* SE_MechScript (old): https://youtu.be/-w7_qgsHzpM

##### The newer scripts are constructed from plug-and-play classes that can be recombined into new files to create whole new scripts for all kinds of different purposes. These classes are not shared between files, as each file has a separate copy of each class. This means that making changes to a class in a new file will not break older code. However, as some scripts contain older versions of these classes, care should be taken to carry over the most up-to-date copy of a class when writing new scripts. If an older version gets carried over, some new features or fixes may not be included.

##### Each class has a version variable in the form of a date, which denotes when that particular copy of that class was last updated. These classes do not (and should not) reference each other in order to preserve their plug-and-play nature. The only exception to this is the Base-class which contains basic quality-of-life features and, for this reason, should always be included in a new script.

#### All files are licensed under the "unlicense" -license, which basicaly means you can use them however you please. Of course, if you do use my code, credit is always appreciated.

#### DISCLAIMER: Nothing here is guaranteed to work in it's current state! This repository is more intended as my personal code stash than a user friendly service!
