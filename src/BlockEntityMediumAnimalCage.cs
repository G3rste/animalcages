﻿using Vintagestory.API.Common;

namespace Animalcages
{
    class BlockEntityMediumAnimalCage : BlockEntityAnimalCage
    {
        protected override void tryGenMesh()
        {
            string variant = Block.CodeEndWithoutParts(0);
            if (variant.Contains("floor") && (variant.Contains("east") || variant.Contains("south")))
            {
                base.tryGenMesh();
                if (currentMesh != null)
                {
                    ModelTransform transform = ModelTransform.NoTransform;
                    if (variant.Contains("east"))
                    {
                        transform.Translation.X -= 0.4f;
                    }
                    if (variant.Contains("south"))
                    {
                        transform.Translation.Z -= 0.6f;
                        transform.Rotation.Y = 90;
                    }
                    currentMesh.ModelTransform(transform);
                }
            }
        }
    }
}
