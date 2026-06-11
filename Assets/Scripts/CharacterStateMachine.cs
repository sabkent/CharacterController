using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public struct CharacterStateMachine : IComponentData
{
    public CharacterStates CurrentState;
    public CharacterStates PreviousState;

    public GroundMoveState GroundMove;
    public AirMoveState AirMove;

    public void Transition(CharacterStates state, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        PreviousState = CurrentState;
        CurrentState = state;
        
        OnStateExit(PreviousState, CurrentState, ref context, ref baseContext, in processor);
        OnStateEnter(CurrentState, PreviousState, ref context, ref baseContext, in processor);
    }

    public void OnStateEnter(CharacterStates current, CharacterStates previous, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        switch (current)
        {
            case CharacterStates.AirMove:
                AirMove.OnStateEnter(previous, ref context, ref baseContext, in processor);
                break;
            case CharacterStates.GroundMove:
                GroundMove.OnStateEnter(previous, ref context, ref baseContext, in processor);
                break;
        }
    }

    public void OnStateExit(CharacterStates previous, CharacterStates current, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        switch (previous)
        {
            case CharacterStates.AirMove:
                AirMove.OnStateExit(current, ref context, ref baseContext, in processor);
                break;
            case CharacterStates.GroundMove:
                GroundMove.OnStateExit(current, ref context, ref baseContext, in processor);
                break;
        }
    }

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        switch (CurrentState)
        {
            case CharacterStates.AirMove:
                AirMove.PhysicsUpdate(ref context, ref baseContext, in processor);
                break;
            case CharacterStates.GroundMove:
                GroundMove.PhysicsUpdate(ref context, ref baseContext, in processor);
                break;
        }
    }

    public void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        switch (CurrentState)
        {
            case CharacterStates.AirMove:
                AirMove.VariableUpdate(ref context, ref baseContext, in processor);
                break;
            case CharacterStates.GroundMove:
                GroundMove.VariableUpdate(ref context, ref baseContext, in processor);
                break;
        }
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
