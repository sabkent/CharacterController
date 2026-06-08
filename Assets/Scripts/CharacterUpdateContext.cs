
    using Unity.Collections;
    using Unity.Entities;

    public struct CharacterUpdateContext
    {
        public int ChunkIndex;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;

        public void OnSystemCreate(ref SystemState state)
        {
            LinkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(isReadOnly: true);
        }

        public void OnSystemUpdate(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            CommandBuffer = commandBuffer.AsParallelWriter();
            LinkedEntityGroupLookup.Update(ref state);
        }

        public void SetChunkIndex(int chunkIndex) => ChunkIndex = chunkIndex;
    }
