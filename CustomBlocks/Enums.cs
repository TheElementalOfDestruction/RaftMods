namespace DestinyCustomBlocks
{
    public enum BlockType
    {
        BED,
        CURTAIN_H,
        CURTAIN_V,
        FLAG,
        RUG_BIG,
        RUG_SMALL,
        SAIL,
        POSTER_H_16_9,
        POSTER_V_9_16,
        POSTER_H_4_3,
        POSTER_V_3_4,
        POSTER_H_3_2,
        POSTER_V_2_3,
        // Special value used for the edit function to not mirror.
        NONE,
        // Special value used for the icons.
        ICON,
    }



    public enum Rotation
    {
        LEFT, // Rotate 90 degrees counter-clockwise.
        FLIP, // Rotate 90 degrees clockwise.
        NONE, // No rotation.
        RIGHT, // Rotate 180 degrees.
    }
}