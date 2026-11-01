#ifndef HB_FOLIAGE_PLANT_DATA_INCLUDED
#define HB_FOLIAGE_PLANT_DATA_INCLUDED

struct HBPlantInstance
{
    float4 row0;
    float4 row1;
    float4 row2;
    float4 inverse0;
    float4 inverse1;
    float4 inverse2;
    float4 sphere;
};

struct HBPlantVisible
{
    uint index;
    float fade;
    float gust;
};

float4x4 HB_PlantWorld(HBPlantInstance instance)
{
    return float4x4(instance.row0, instance.row1, instance.row2, float4(0.0, 0.0, 0.0, 1.0));
}

#endif
