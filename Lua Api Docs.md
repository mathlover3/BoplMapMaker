## Sequence
in case you are new to lua...
the term sequence is used to denote a table where the set of all positive numeric keys is equal to {1..n} for some integer n, which is called the length of the sequence.

## Notes
the camra has a Xmin of -97.27, a XMax of 97.6, a YMax of 40, a Ymin of -26, and waterHeight is at -11.3

IF YOU BREAK THE GAME OR FREEZE THE GAME WITH LUA BY DOING SOMTHING ABSURD LIKE PUTTING A PLATFORM 1,000,000 UNITS UP ITS NOT MY FAULT!
if the game errors do to a call that involves lua then it is infact a bug and should be reported. also dont worry you cant effect anything outside of bopl. so you cant get a virus from custom bopl maps lol.
## Global Funcsons
the following are global funcsons.
for the following angle is in degrees.
the type before the funcson are its return type.

Red, Green, Blue and Alpha are all number between 0 and 1.
BoplBody SpawnArrow(number posX, number posY, number scale, number StartVelX, number StartVelY, number Red, number Green, number Blue, number Alpha)

BoplBody SpawnGrenade(number posX, number posY, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none SpawnAbilityPickup(number posX, number posY, number scale, number StartVelX, number StartVelY)

BoplBody SpawnSmokeGrenade(number posX, number posY, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none SpawnExplosion(number posX, number posY, number scale)

type can be the following: "grass", "snow", "ice", "space", "slime", "robot" if it isnt one of those it will throw a error.
only if the type is slime does R, G, B and A mater
R, G, B and A are numbers between 0 and 1.
Platform SpawnBoulder(number posX, number posY, number scale, number StartVelX, number StartVelY, number StartAngularVelocity, string type, number R, number G, number B, number A)

Width and Height are distances from a edge to the center - Radius. To calculate the true Width/Height in bopl units you do (Width + Radius)*2. same for Height but with Height instead of Width.
R, G, B and A are numbers between 0 and 1.
Rot is in degress as normal.
Platform SpawnPlatform(number posX, number posY, number Width, number Height, number Radius, number Rot, number R, number G, number B, number A)

sends a raycast from a point that can only hit RoundedRects (platforms and matchoman boulders) into the world and returning the distance it went and the RoundedRect it hit.
the number is the distance it travaled before hitting anything. returns a very big negitive number if it doesnt hit anything
note that Platform may be nil if it didnt hit anything.
0 deg is right and 90 is up.
number, Platform RaycastRoundedRect(number posX, number posY, number angle, number maxDist)

gets the closest player to that posison. returns nil if all the players are gone (begiening/end of game)
Player GetClosestPlayer(number posX, number posY)

returns then number of platforms then all platforms (including boulders)
dont rely on the order of the platforms being consistent. the order of the platforms might change due to outer mods or me changing stuff. dont blame me if your code breaks because the order changes
number, Sequence (of Platforms) GetAllPlatforms()
dont rely on the order of the players being consistent. the order of the platforms might change due to outer mods or me changing stuff. dont blame me if your code breaks because the order changes
returns then number of players then all players. note that there may be no entrys if theres no players.
number, Sequence (of Players) GetAllPlayers()
dont rely on the order of the BoplBodys being consistent. the order of the platforms might change due to outer mods or me changing stuff. dont blame me if your code breaks because the order changes
returns then number of BoplBodys then all BoplBodys. will not return BoplBodys that havent been initialized yet or is being destroyed on that frame.
number, Sequence (of BoplBodys) GetAllBoplBodys()

for some reson if you shoot a blink under the water it prevents stuff from reflecting off it for a bit??? no clue why.
for normal blink minPlayerDuration = 0.5, WallDuration = 4, WallDelay = 1, WallShake = 0.3
none ShootBlink(number posX, number posY, number Angle, number minPlayerDuration, number WallDuration, number WallDelay, number WallShake)

for normal grow ScaleMultiplyer = 0.8, PlayerMultiplyer = 0.8, blackHoleGrowth = 50
none ShootGrow(number posX, number posY, number Angle, number ScaleMultiplyer, number PlayerMultiplyer, number blackHoleGrowth)

for normal shrink ScaleMultiplyer = -0.8, PlayerMultiplyer = -0.8, blackHoleGrowth = -50
none ShootShrink(number posX, number posY, number Angle, number ScaleMultiplyer, number PlayerMultiplyer, number blackHoleGrowth)

gets the time in seconds that has pased sence the last frame.
number GetDeltaTime()

gets the time sence the level loaded. this includes the time before the players have spawned in.
number GetTimeSinceLevelLoad()

returns true if time is stoped
bool IsTimeStopped()

gets the value of the logic gate input with that id. uses 1 based indexing. returns a error if the id is > then the number of inputs
bool GetInputValueWithId(number id)

sets the value of the logic gates output with that id. returns a error if the id is > then the number of outputs. the output will stay the value you set untill you set it agien.
none SetOutputWithId(number id, bool value)

gets a file from inside the map file with the given name including the exstencon. exsample name is "File.dat". it cant acsess files inside founders inside the map (maps are just zips in descise). returns a Sequence of bytes or a error if the file doesnt exsit.
Sequence (of bytes) GetFileFromMapFile(string FileName)
## Vec2
the type before the funcson are its return type. Vec2 is just a shorthand for outputing 2 numbers x, y

## Player
the following are funcsons of the Player type (returned by some funcsons) (tecnicly its a userData type but it acts like its its own type so we can think of it like it is)
number Player.GetSpeed()
number Player.GetGroundedSpeed()
number Player.GetMaxSpeed()
number Player.GetJumpStrength()
number Player.GetAccel()
number Player.GetGravityAccel()
number Player.GetGravityMaxFallSpeed()
number Player.GetJumpExtraXStrength()
number Player.GetJumpKeptMomentum()
number Player.GetAirAccel()
number Player.GetMass()
Vec2 Player.GetPosition()
none Player.SetPosition(number PosX, number PosY)
none Player.SetSpeed(number NewValue)
none Player.SetGroundedSpeed(number NewValue)
none Player.SetMaxSpeed(number NewValue)
none Player.SetJumpStrength(number NewValue)
none Player.SetAccel(number NewValue)
none Player.SetGravityAccel(number NewValue)
none Player.SetGravityMaxFallSpeed(number NewValue)
none Player.SetJumpExtraXStrength(number NewValue)
none Player.SetJumpKeptMomentum(number NewValue)
none Player.SetAirAccel(number NewValue)
none Player.SetMass(number NewValue)
none Player.AddForce(number ForceX, number ForceY)

gets the ability in that slot. valid indexs are 1, 2 and 3. ability slots are in the order of left, right, middle.
string Player.GetAbility(number index)

sets the ability in that slot. valid indexs are 1, 2 and 3.
valid abilitys are {"Roll", "Dash", "Grenade", "Bow", "Engine", "Blink", "Gust", "Grow", "Rock", "Missle", "Spike", "TimeStop", "SmokeGrenade", "Platform", "Revive", "Shrink", "BlackHole", "Invisibility", "Meteor", "Macho", "Push", "Tesla", "Mine", "Teleport", "Drill", "Grapple", "Beam", "Duplicator"}
if there is 1 ability it will ignore index and add it to the player. same for if theres 2 abilitys. this is due to how the game works. abilitys that are added also have there cooldowns reset
void Player.SetAbility(number index, string ability, bool PlayAbilityPickupSound)

number Player.GetAbilityCount()

returns 1,000,000 if theres no ability in that slot. if theres a ability in the slot it returns the time remaining of the cooldown. may be negitive if the ability is ready
number Player.GetAbilityCooldownRemaining(number index)

sets the cooldown remaining for the ability in that slot. does nothing if theres no ability in the slot
none Player.SetAbilityCooldownRemaining(number index, number NewRemainingCooldown)

returns 1,000,000 if theres no ability in that slot. if theres a ability in the slot it returns the time it takes for the ability to recharge after use.
number GetAbilityMaxCooldown(number index)

returns true if the player is currently disappeared from blink
bool IsDisappeared()

returns the platform the player is on. nil if they arent on a platform
Platform GetPlatform()

returns "Player"
string Player.GetClassType()

## Platform
the following are the funcsons of the Platform type.
note that Platform is for both Platforms and Boulders.
returns "Platform"
string Platform.GetClassType()

home is basicly what posison it would like to be in. its what the springs try to get the platform to. GetHome, GetHomeRot, SetHome, SetHomeRot dont work on Boulders and will cause a error.
Vec2 Platform.GetHome()
number Platform.GetHomeRot()
number Platform.GetScale()
setting scale outside of the normal scale range will set it to the minimum/maximum allowed scale
none Platform.SetScale(number scale)
none Platform.SetHome(number posX, number posY)
none Platform.SetHomeRot(number NewRot)
none Platform.ShakePlatform(number Duratson, number ShakeAmount)
none Platform.DropAllPlayers(number DropForce)

gets the scale that the platform will automaticly strink too when it hasnt been grown in a while. this is normaly its starting scale.
number GetBaseScale()

sets the scale that the platform will automaticly strink too when it hasnt been grown in a while. this is normaly its starting scale.
none SetBaseScale(number scale)

gets the platform type. can return "grass", "snow", "ice", "space", "robot", "slime". "custom"
string GetPlatformType()

may be nil.
BoplBody Platform.GetBoplBody()

bool Platform.IsBoulder()

this is true for custom shaped platforms,  platforms created by the platform ability and platforms created with lua.
bool Platform.IsResizable()

resizes the platform. only works if Platform.IsResizable() is true. Width and Height are distances from a edge to the center - Radius. To calculate the true Width/Height in bopl units you do (Width + Radius)*2. same for Height but with Height instead of Width.
none ResizePlatform(number Width, number Height, number Radius)

gets the Width, Height and Radius of the platform in the same way as ResizePlatform takes in. this works on all platforms and boulders.
number, number, number GetPlatformSize()

gets the True Width and Height taking into acount rotatsons. returns Width, Height
number, number GetTrueWidthAndHeight()

# BoplBody
this is the funcsons the BoplBody has.
returns "BoplBody"
string BoplBody.GetClassType()
bool HasBeenInitialized()
bool IsBeingDestroyed()
Vec2 BoplBody.GetPos()
number BoplBody.GetRot()

GetScale, GetMass, GetVelocity, AddForce, SetScale, SetVelocity and SetMass will error out if it hasnt been initialized or if its being destroyed (this only is the case for 1 frame and then the BoplBody becomes nil.). check HasBeenInitialized and IsBeingDestroyed before calling them.
number BoplBody.GetScale()
number BoplBody.GetMass()
Vec2 BoplBody.GetVelocity()
none BoplBody.SetPos(number PosX, number PosY)
none BoplBody.SetRot(number Rot)
setting scale outside of the normal scale range will set it to the minimum/maximum allowed scale
none BoplBody.SetScale(number Scale)
none BoplBody.SetVelocity(number VelX, number VelY)
none BoplBody.SetMass(number Mass)
none BoplBody.AddForce(number ForceX, number ForceY)
none BoplBody.Destroy()

returns true if the BoplBody is currently disappeared from blink or a DisappearPlatformOnSignal.
bool IsDisappeared()

R, G, B and A are numbers between 0 and 1.
may not do anything on some objects.
BoplBody.SetColor(number R, number G, number B, number A)

can return "Arrow", "RocketEngine", "Mine", "Telsa", "AbilityPickup", "Missile", "MatchoBoulder", "Spike", "Rock", "Smoke", "Smoke Grenade", "Grenade", "Platform", "Unknown/Modded"
string BoplBody.GetObjectType()

## Errors
When a script has a error/fails to parse it logs the error to the consule. for runtime errors it says where in the code the error happend, what script it happend in, and what the error is. for a parseing error it says aproxamently where the error is. print() also logs the first pramiter to the consule

## Defult Lua Apis
you get the following built in lua apis:
The global constants: "_G", "_VERSION" and "_MOONSHARP",
The table iterators: "next", "ipairs" and "pairs",
The metatable methods : "setmetatable", "getmetatable", "rawset", "rawget", "rawequal" and "rawlen",
The string package,
The table package,
The math package,
The bit32 package,
The error handling methods: "pcall" and "xpcall",
The coroutine package.