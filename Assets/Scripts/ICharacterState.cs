using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public interface ICharacterState
{
    void OnStateEnter(CharacterStates previousState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor);
    void OnStateExit(CharacterStates nextState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor);

    void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor);
    void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor);

    (Entity cameraTarget, bool calculateUpFromGravity) GetCameraParameters(in Character character);
    
    float3 GetMoveFromInput(in PlayerInput input, quaternion cameraRotation);
}
