using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Biome
{
    public string BiomeName;

    [Space(5)]
    public float PerlinScale1;
    public float PerlinYScale1;
    public float PerlinStrength1;

    [Space(5)]
    public float PerlinScale2;
    public float PerlinYScale2;
    public float PerlinStrength2;

    [Space(5)]
    public float PerlinScale3;
    public float PerlinYScale3;
    public float PerlinStrength3;

    [Space(5)]
    public AnimationCurve HightCurve;
    public AnimationCurve MagnitudeCurve;

    [Space(5)]
    public Color color;
}

public class WorldChunk
{
    public float[,,] WorldValue = new float[16 + 1, 256 + 1, 16 + 1];
    public int ChunkPosition_x;
    public int ChunkPosition_z;
    public int BiomeID;
}

public struct SubChunk
{
    public float value;
};
public class WorldGenerator : MonoBehaviour
{
    public List<WorldChunk> WorldChunks = new List<WorldChunk>();
    public ComputeShader computeShader;

    [Space(10)]

    public Biome[] biomes;

    [Space(10)]

    public float biomeScale;

    static public float SurfaceLevel = 0;

    public GameObject debugcube;

    public float seedoffset_x;
    public float seedoffset_z;

    public RenderTexture renderTexture;

    int[,] biomeIdTable =   {   
                            {0, 1},
                            {2, 3}
                            };

    void Awake()
    {
        seedoffset_x = Random.Range(-100f, 100f);
        seedoffset_z = Random.Range(-100f, 100f);

        /*
        float normalizer = PerlinStrength1 + PerlinStrength2 + PerlinStrength3;
        PerlinStrength1 /= normalizer;
        PerlinStrength2 /= normalizer;
        PerlinStrength3 /= normalizer;
        */

        /*for (int x = -2; x < 2; x++)
        {
            for (int z = -2; z < 2; z++)
            {
                WorldChunks.Add(GenerateChunkGPU(x, z));
            }
        }*/
        //WorldChunks.Add(GenerateChunkGPU(0, 0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GenerateBiome(int chunk_x, int chunk_z)
    {
        Vector2 value = new Vector2(Perlin.Noise(((float)chunk_x + seedoffset_x) * biomeScale, ((float)chunk_z + seedoffset_z) * biomeScale), Perlin.Noise(((float)chunk_x + seedoffset_z) * biomeScale, ((float)chunk_z + seedoffset_x) * biomeScale));

        value = (value + Vector2.one) / 2;

        return biomeIdTable[Mathf.FloorToInt(value.x * 2), Mathf.FloorToInt(value.y * 2)];
    }

    public WorldChunk GenerateChunk(int chunk_x, int chunk_z)
    {
        int biomeID = GenerateBiome(chunk_x, chunk_z);
        int neighborBiome_xp = GenerateBiome(chunk_x + 1, chunk_z);
        int neighborBiome_xm = GenerateBiome(chunk_x - 1, chunk_z);
        int neighborBiome_zp = GenerateBiome(chunk_x, chunk_z + 1);
        int neighborBiome_zm = GenerateBiome(chunk_x, chunk_z - 1);
        Biome biome = biomes[biomeID];
        Biome blendbiome;

        float starttime = Time.realtimeSinceStartup;
        //create chunk
        WorldChunk worldChunk = new WorldChunk();

        //set chunk metadata
        worldChunk.ChunkPosition_x = chunk_x;
        worldChunk.ChunkPosition_z = chunk_z;
        worldChunk.BiomeID = biomeID;

        //generate the world
        Vector3 offset = new Vector3(chunk_x * 16 + seedoffset_x, 0, chunk_z * 16 + seedoffset_z);
        float value = 0;
        float blendvalue = 0;
        float finalvalue = 0;
        Vector3 worldpos;
        for (int x = 0; x < 16 + 1; x++)
        {
            for (int y = 0; y < 256 + 1; y++)
            {
                for (int z = 0; z < 16 + 1; z++)
                {
                    worldpos = new Vector3(x + offset.x, y + offset.y, z + offset.z);
                    value = Perlin.Noise(worldpos.x * biome.PerlinScale1, worldpos.y * biome.PerlinScale1 * biome.PerlinYScale1, worldpos.z * biome.PerlinScale1) * biome.PerlinStrength1 + Perlin.Noise(worldpos.x * biome.PerlinScale2, worldpos.y * biome.PerlinScale2 * biome.PerlinYScale2, worldpos.z * biome.PerlinScale2) * biome.PerlinStrength2 + Perlin.Noise(worldpos.x * biome.PerlinScale3, worldpos.y * biome.PerlinScale3 * biome.PerlinYScale3, worldpos.z * biome.PerlinScale3) * biome.PerlinScale3;
                    
                    value = Mathf.Clamp(value * biome.MagnitudeCurve.Evaluate((float)y / 256), -1, 1);
                    value = ((value + 1) * biome.HightCurve.Evaluate((float)y / 256)) - 1;

                    finalvalue = value;

                    //biome blend
                    if(x > 7 && neighborBiome_xp != biomeID)
                    {
                        blendbiome = biomes[neighborBiome_xp];
                        blendvalue = Perlin.Noise(worldpos.x * blendbiome.PerlinScale1, worldpos.y * blendbiome.PerlinScale1 * blendbiome.PerlinYScale1, worldpos.z * blendbiome.PerlinScale1) * blendbiome.PerlinStrength1 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale2, worldpos.y * blendbiome.PerlinScale2 * blendbiome.PerlinYScale2, worldpos.z * blendbiome.PerlinScale2) * blendbiome.PerlinStrength2 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale3, worldpos.y * blendbiome.PerlinScale3 * blendbiome.PerlinYScale3, worldpos.z * blendbiome.PerlinScale3) * blendbiome.PerlinScale3;
                        finalvalue = Mathf.Lerp(finalvalue, blendvalue, (float)(x - 8) / 8f);
                    }
                    if (x < 8 && neighborBiome_xm != biomeID)
                    {
                        blendbiome = biomes[neighborBiome_xm];
                        blendvalue = Perlin.Noise(worldpos.x * blendbiome.PerlinScale1, worldpos.y * blendbiome.PerlinScale1 * blendbiome.PerlinYScale1, worldpos.z * blendbiome.PerlinScale1) * blendbiome.PerlinStrength1 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale2, worldpos.y * blendbiome.PerlinScale2 * blendbiome.PerlinYScale2, worldpos.z * blendbiome.PerlinScale2) * blendbiome.PerlinStrength2 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale3, worldpos.y * blendbiome.PerlinScale3 * blendbiome.PerlinYScale3, worldpos.z * blendbiome.PerlinScale3) * blendbiome.PerlinScale3;
                        finalvalue = Mathf.Lerp(finalvalue, blendvalue, (float)(7 - x) / 8f);
                    }
                    if (z > 7 && neighborBiome_zp != biomeID)
                    {
                        blendbiome = biomes[neighborBiome_zp];
                        blendvalue = Perlin.Noise(worldpos.x * blendbiome.PerlinScale1, worldpos.y * blendbiome.PerlinScale1 * blendbiome.PerlinYScale1, worldpos.z * blendbiome.PerlinScale1) * blendbiome.PerlinStrength1 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale2, worldpos.y * blendbiome.PerlinScale2 * blendbiome.PerlinYScale2, worldpos.z * blendbiome.PerlinScale2) * blendbiome.PerlinStrength2 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale3, worldpos.y * blendbiome.PerlinScale3 * blendbiome.PerlinYScale3, worldpos.z * blendbiome.PerlinScale3) * blendbiome.PerlinScale3;
                        finalvalue = Mathf.Lerp(finalvalue, blendvalue, (float)(z - 8) / 8f);
                    }
                    if (z < 8 && neighborBiome_zm != biomeID)
                    {
                        blendbiome = biomes[neighborBiome_zm];
                        blendvalue = Perlin.Noise(worldpos.x * blendbiome.PerlinScale1, worldpos.y * blendbiome.PerlinScale1 * blendbiome.PerlinYScale1, worldpos.z * blendbiome.PerlinScale1) * blendbiome.PerlinStrength1 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale2, worldpos.y * blendbiome.PerlinScale2 * blendbiome.PerlinYScale2, worldpos.z * blendbiome.PerlinScale2) * blendbiome.PerlinStrength2 + Perlin.Noise(worldpos.x * blendbiome.PerlinScale3, worldpos.y * blendbiome.PerlinScale3 * blendbiome.PerlinYScale3, worldpos.z * blendbiome.PerlinScale3) * blendbiome.PerlinScale3;
                        finalvalue = Mathf.Lerp(finalvalue, blendvalue, (float)(7 - z) / 8f);
                    }

                    worldChunk.WorldValue[x,y,z] = finalvalue;
                }
            }
        }

        Debug.Log("Chunk (" + chunk_x + ", " + chunk_z + ", biomeID: " + biomeID +") Generated in " + (Time.realtimeSinceStartup - starttime).ToString() + "s !");
        return worldChunk;
    }

    public WorldChunk GenerateChunkGPU(int chunk_x, int chunk_z)
    {
        Biome biome = biomes[GenerateBiome(chunk_x, chunk_z)];

        float starttime = Time.realtimeSinceStartup;
        //create chunk
        WorldChunk worldChunk = new WorldChunk();

        //set chunk metadata
        worldChunk.ChunkPosition_x = chunk_x;
        worldChunk.ChunkPosition_z = chunk_z;

        //initialize chunk
        /*
        renderTexture = new RenderTexture(320, 320, 16);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        */

        
        SubChunk[] subChunks;
        subChunks = new SubChunk[4 * 64 * 4];
        for(int i = 0; i < 4 * 64 * 4; i++)
        {
            subChunks[i].value = 0;
        }
        

        //generate chunk
        int subChunkSize = sizeof(float) * 5 * 4 * 5;
        int totalSize = subChunkSize;

        
        ComputeBuffer chunksBuffer = new ComputeBuffer(subChunks.Length, /*totalSize*/ sizeof(float) * 128);
        chunksBuffer.SetData(subChunks);

        computeShader.SetBuffer(0, "subChunks", chunksBuffer);

        //computeShader.SetTexture(0, "Result", renderTexture);

        computeShader.SetInt("chunk_x", chunk_x);
        computeShader.SetInt("chunk_z", chunk_z);
        computeShader.SetFloat("seedoffset_x", seedoffset_x);
        computeShader.SetFloat("seedoffset_z", seedoffset_z);
        computeShader.SetFloat("PerlinScale1", biome.PerlinScale1);
        computeShader.SetFloat("PerlinStrength1", biome.PerlinStrength1);
        computeShader.SetFloat("PerlinYScale1", biome.PerlinYScale1);
        computeShader.SetFloat("PerlinScale2", biome.PerlinScale2);
        computeShader.SetFloat("PerlinStrength2", biome.PerlinStrength2);
        computeShader.SetFloat("PerlinYScale2", biome.PerlinYScale2);
        computeShader.SetFloat("PerlinScale3", biome.PerlinScale3);
        computeShader.SetFloat("PerlinStrength3", biome.PerlinStrength3);
        computeShader.SetFloat("PerlinYScale3", biome.PerlinYScale3);
        computeShader.SetFloats("MagnitudeCurve", AnimationCurveToFloat(biome.MagnitudeCurve));
        computeShader.SetFloats("HeightCurve", AnimationCurveToFloat(biome.HightCurve));
        
        //computeShader.Dispatch(0, 4, 64, 4);

        //Get data!

        //Texture2D texture = toTexture2D(renderTexture);

        //chunksBuffer.GetData(subChunks);

        //assemble chunk

        float value;

        for (int x = 0; x < 4; x++)
        {
            //Debug.Log(x);
            for (int y = 0; y < 64; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    computeShader.SetInt("subchunk_x", x);
                    computeShader.SetInt("subchunk_y", y);
                    computeShader.SetInt("subchunk_z", z);

                    computeShader.Dispatch(0, x == 3 ? 5 : 4, 4, z == 3 ? 5 : 4);
                    chunksBuffer.GetData(subChunks);

                    int i = 0;
                    for (int cx = 0; cx < (x < 3 ? 4 : 5); cx++)
                    {
                        for (int cy = 0; cy < 4; cy++)
                        {
                            for (int cz = 0; cz < (z < 3 ? 4 : 5); cz++)
                            {
                                worldChunk.WorldValue[(x * 4) + cx, (y * 4) + cy, (z * 4) + cz] = subChunks[cx + cy * 5 + cz * 5 * 4].value;
                                
                                /*
                                value = texture.GetPixel(i % 320, Mathf.FloorToInt(i / 320)).r;
                                worldChunk.WorldValue[(x * 4) + cx, (y * 4) + cy, (z * 4) + cz] = value;
                                */
                                
                                /*
                                if (i == 0 && x == 0)
                                {
                                    Debug.Log(subChunks[cx + cy * 5 + cz * 5 * 4].value);
                                }
                                */
                                
                                i++;

                            }
                        }
                    }  
                    

                }
            }
        }

        chunksBuffer.Dispose();
        
        //renderTexture.Release();

        Debug.Log("Chunk (" + chunk_x + ", " + chunk_z + ") GPU Generated in " + (Time.realtimeSinceStartup - starttime).ToString() + "s !");
        return worldChunk;
    }

    float[] AnimationCurveToFloat(AnimationCurve curve)
    {
        float[] table = new float[64];

        for(int i = 0; i < 64; i++)
        {
            table[i] = curve.Evaluate((float)i / 64f);
        }

        return table;
    }

    public Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D dest = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false);
        dest.Apply(false);
        Graphics.CopyTexture(rTex, dest);
        return dest;
    }
}
