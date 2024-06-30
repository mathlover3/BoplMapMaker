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
note that Platform/Boulder may be nil if it didnt hit anything.
to see if its a platform or a boulder class check if its nil and if not then use GetClassType to see witch it is.
number, Platform/Boulder RaycastRoundedRect(number posX, number posY, number angle, number maxDist)

gets the closest player to that posison. returns nil if all the players are gone (begiening/end of game)
Player GetClosestPlayer(number posX, number posY)


the following are funcsons of the Player type (returned by some funcsons) (tecnicly its a userData type but it acts like its its own type so we can think of it like it is)
the type before the funcson are its return type. Vec2's is just a shorthand for a table with a number value for "x" and "y", any outer values in the table are ignored.

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
Vec2 Player.GetVelocity()
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
string Player.GetClassType()

the following are the funcsons of the Platform type.
string Platform.GetClassType()
Vec2 GetPos()
number GetRot()
Vec2 GetHome()
number GetHomeRot()