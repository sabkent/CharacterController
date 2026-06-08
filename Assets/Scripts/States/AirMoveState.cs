using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public struct AirMoveState: ICharacterState
{
    public void OnStateEnter(CharacterStates previousState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        
    }

    public void OnStateExit(CharacterStates nextState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        
    }

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        throw new System.NotImplementedException();
    }

    public void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        throw new System.NotImplementedException();
    }

    public (Entity cameraTarget, bool calculateUpFromGravity) GetCameraParameters(in Character character)
    {
        throw new System.NotImplementedException();
    }

    public float3 GetMoveFromInput(in PlayerInput input, quaternion cameraRotation)
    {
        throw new System.NotImplementedException();
    }
}
