## Sequence
in case you are new to lua...
the term sequence is used to denote a table where the set of all positive numeric keys is equal to {1..n} for some integer n, which is called the length of the sequence.

## Notes
the camra has a Xmin of -97.27, a XMax of 97.6, a YMax of 40, a Ymin of -26, and waterHeight is at -11.3
## Global Funcsons
the following are global funcsons.
for the following angle is in degrees.
the type before the funcson are its return type.

none SpawnArrow(number posX, number posY, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none SpawnGrenade(number posX, number posY, number angle, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none SpawnAbilityPickup(number posX, number posY, number scale, number StartVelX, number StartVelY)

none SpawnSmokeGrenade(number posX, number posY, number angle, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none SpawnExplosion(number posX, number posY, number scale)

sends a raycast from a point that can only hit RoundedRects (platforms and matchoman boulders) into the world and returning the distance it went and the RoundedRect it hit.
the number is the distance it travaled before hitting anything. returns a very big negitive number if it doesnt hit anything
note that Platform may be nil if it didnt hit anything.
0 deg is right and 90 is up.
number, Platform RaycastRoundedRect(number posX, number posY, number angle, number maxDist)

gets the closest player to that posison. returns nil if all the players are gone (begiening/end of game)
Player GetClosestPlayer(number posX, number posY)
returns all platforms (including boulders)
Sequence (of Platforms) GetAllPlatforms()

for normal blink minPlayerDuration = 0.5, WallDuration = 4, WallDelay = 1, WallShake = 0.3
none ShootBlink(number posX, number posY, number Angle, number minPlayerDuration, number WallDuration, number WallDelay, number WallShake)

for normal grow ScaleMultiplyer = 0.8, PlayerMultiplyer = 0.8, blackHoleGrowth = 50
none ShootGrow(number posX, number posY, number Angle, number ScaleMultiplyer, number PlayerMultiplyer, number blackHoleGrowth)

for normal shrink ScaleMultiplyer = -0.8, PlayerMultiplyer = -0.8, blackHoleGrowth = -50
none ShootShrink(number posX, number posY, number Angle, number ScaleMultiplyer, number PlayerMultiplyer, number blackHoleGrowth)

gets the time in seconds that has pased sence the last frame.
number GetDeltaTime()

gets the time sence the level loaded. this includes the time before the players have spawned in.
number GetTimeSenceLevelLoad()

gets the value of the logic gate input with that id. uses 1 based indexing. returns a error if the id is > then the number of inputs
bool GetInputValueWithId(number id)

sets the value of the logic gates output with that id. returns a error if the id is > then the number of outputs
none SetOutputWithId(number id, bool value)

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
Vec2 Player.GetPosition()
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
returns "Player"
string Player.GetClassType()

## Platform
the following are the funcsons of the Platform type.
note that Platform is for both Platforms and Boulders.
returns "Platform"
string Platform.GetClassType()
Vec2 Platform.GetPos()
number Platform.GetRot()

home is basicly what posison it would like to be in. its what the springs try to get the platform to. GetHome, GetHomeRot, SetHome, SetHomeRot dont work on Boulders and will cause a error.
Vec2 Platform.GetHome()
number Platform.GetHomeRot()
number Platform.GetScale()
none Platform.SetScale(number scale)
none Platform.SetHome(number posX, number posY)
none Platform.SetHomeRot(number NewRot)
none Platform.ShakePlatform(number Duratson, number ShakeAmount)
none Platform.DropAllPlayers(number DropForce)
BoplBody Platform.GetBoplBody()
bool Platform.IsBoulder()

# BoplBody
this is the funcsons the BoplBody has.
returns "BoplBody"
string BoplBody.GetClassType()
Vec2 BoplBody.GetPos()
number BoplBody.GetRot()
number BoplBody.GetScale()
none BoplBody.SetPos(number PosX, number PosY)
none BoplBody.SetRot(number Rot)
none BoplBody.SetScale(number Scale)
none BoplBody.SetVelocity(number VelX, number VelY)
none BoplBody.AddForce(number ForceX, number ForceY)\

can return "Arrow", "RocketEngine", "Mine", "Telsa", "AbilityPickup", "Missile", "MatchoBoulder", "Spike", "Rock", "Smoke", "Smoke Grenade", "Grenade", "Unknown/Modded"
string BoplBody.GetObjectType()