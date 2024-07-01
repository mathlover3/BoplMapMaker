## Sequence
in case you are new to lua...
the term sequence is used to denote a table where the set of all positive numeric keys is equal to {1..n} for some integer n, which is called the length of the sequence.


## Global Funcsons
the following are global funcsons.
for the following angle is in degrees.
the type before the funcson are its return type.

none/nil SpawnArrow(number posX, number posY, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none/nil SpawnGrenade(number posX, number posY, number angle, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none/nil SpawnAbilityPickup(number posX, number posY, number scale, number StartVelX, number StartVelY)

none/nil SpawnSmokeGrenade(number posX, number posY, number angle, number scale, number StartVelX, number StartVelY, number StartAngularVelocity)

none/nil SpawnExplosion(number posX, number posY, number scale)

sends a raycast from a point that can only hit RoundedRects (platforms and matchoman boulders) into the world and returning the distance it went and the RoundedRect it hit.
the number is the distance it travaled before hitting anything. returns a very big negitive number if it doesnt hit anything
note that Platform may be nil if it didnt hit anything.
0 deg is right and 90 is up.
number, Platform RaycastRoundedRect(number posX, number posY, number angle, number maxDist)

gets the closest player to that posison. returns nil if all the players are gone (begiening/end of game)
Player GetClosestPlayer(number posX, number posY)
returns all platforms (including boulders)
Sequence (of Platforms) GetAllPlatforms()


## Vec2
the type before the funcson are its return type. Vec2 is just a shorthand for a table with a number value for "x" and "y"

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
none/nil Player.SetSpeed(number NewValue)
none/nil Player.SetGroundedSpeed(number NewValue)
none/nil Player.SetMaxSpeed(number NewValue)
none/nil Player.SetJumpStrength(number NewValue)
none/nil Player.SetAccel(number NewValue)
none/nil Player.SetGravityAccel(number NewValue)
none/nil Player.SetGravityMaxFallSpeed(number NewValue)
none/nil Player.SetJumpExtraXStrength(number NewValue)
none/nil Player.SetJumpKeptMomentum(number NewValue)
none/nil Player.SetAirAccel(number NewValue)

if you set it to false it disapears and if you set it to true it reapears. (also is set by disapearing platforms and blink)
none/nil Player.SetActive(bool active)
bool Player.IsActive()
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

none/nil Platform.SetScale(number scale)
none/nil Platform.SetHome(number posX, number posY)
none/nil Platform.SetHomeRot(number NewRot)
none/nil Platform.ShakePlatform(number Duratson, number ShakeAmount)
none/nil Platform.DropAllPlayers(number DropForce)
BoplBody/nil Platform.GetBoplBody()
if you set it to false it disapears and if you set it to true it reapears. (also is set by disapearing platforms and blink)
none/nil Platform.SetActive(bool active)
bool Platform.IsActive()
bool Platform.IsBoulder()

# BoplBody
this is the funcsons the BoplBody has.
returns "BoplBody"
string BoplBody.GetClassType()
Vec2 BoplBody.GetPos()
number BoplBody.GetRot()
number BoplBody.GetScale()
none/nil BoplBody.SetPos(number PosX, number PosY)
none/nil BoplBody.SetRot(number Rot)
none/nil BoplBody.SetScale(number Scale)
none/nil BoplBody.SetVelocity(number VelX, number VelY)
none/nil BoplBody.AddForce(number ForceX, number ForceY)