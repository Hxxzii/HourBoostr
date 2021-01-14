## This is a fork of SingleBoostr found in HourBoostr - you can find the original author's repository [here](https://github.com/Ezzpify/HourBoostr)
### This fork aims for simplicity (console-only interface, hopefully low RAM usage)
### If you have any problems with this fork, make an issue in this repository, NOT the original repository.
#### Libraries used: INIParser and Steam4NET
#### The only source code used from the original repository is [this file](https://github.com/Ezzpify/HourBoostr/blob/master/SingleBoostr/SingleBoostrGame/SingleBoostrGame/Program.cs) and has been incorporated [here](https://github.com/Hxxzii/SingleBoostr/blob/master/SingleBoostr.IdlingProcess/Program.cs)
#### Furthermore, this fork is licensed under the same license as the original repository: [GNU GPL-v3.0](https://github.com/Hxxzii/HourBoostr/blob/master/LICENSE)
----
Build instructions:
(Optional) Build the Steam4NET library, and add as a reference to `SingleBoostr.IdlingProcess` - this is optional as this repository already comes with a compiled version of Steam4NET with optimizations

Compile with Visual Studio 2019 (NET Framework 4.8 required)

Afterwards, copy the compiled `SingleBoostr.IdlingProcess` binaries and put it in the same folder/directory with the `SingleBoostr.Client` binaries

And that's it!
