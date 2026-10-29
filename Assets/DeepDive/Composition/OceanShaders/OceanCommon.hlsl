#ifndef DEEPDIVE_OCEAN_COMMON
#define DEEPDIVE_OCEAN_COMMON
float _OceanTime, _OceanWind, _OceanDaylight, _OceanSurfaceY, _OceanReflectionValid;
float4 _OceanSunDirection, _OceanSunColor;
float4x4 _OceanReflectionVP;
float4 _OceanActors[8], _OceanVelocities[8];
int _OceanActorCount;
float OceanHash(float2 p) { float3 q=frac(p.xyx*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z); }
float3 OceanNoiseGradient(float2 p)
{
    float2 i=floor(p),f=frac(p),u=f*f*(3-2*f),du=6*f*(1-f);
    float a=OceanHash(i),b=OceanHash(i+float2(1,0)),c=OceanHash(i+float2(0,1)),d=OceanHash(i+1);
    return float3(lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y),lerp(b-a,d-c,u.y)*du.x,lerp(c-a,d-b,u.x)*du.y);
}
float OceanNoise(float2 p)
{
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(OceanHash(i),OceanHash(i+float2(1,0)),f.x),
        lerp(OceanHash(i+float2(0,1)),OceanHash(i+1),f.x),f.y);
}
float OceanHeight(float2 p)
{
    float shore=lerp(.16,1,smoothstep(-12,-2,p.y));
    return shore*lerp(.55,1,_OceanWind)*(.105*sin(dot(p,float2(.48,.19))-_OceanTime*1.15)+
        .045*sin(dot(p,float2(-.25,.73))-_OceanTime*1.7)+.02*sin(dot(p,float2(1.4,.7))-_OceanTime*2.15));
}
float3 OceanNormal(float2 p)
{
    float2 g=.105*float2(.48,.19)*cos(dot(p,float2(.48,.19))-_OceanTime*1.15)+
        .045*float2(-.25,.73)*cos(dot(p,float2(-.25,.73))-_OceanTime*1.7)+
        .02*float2(1.4,.7)*cos(dot(p,float2(1.4,.7))-_OceanTime*2.15);
    float footprint=length(fwidth(p));
    g*=lerp(.55,1,_OceanWind)*rcp(1+footprint*.4);
    float detail=rcp(1+footprint*4);
    g+=detail*(OceanNoiseGradient(p*.85+float2(_OceanTime*.22,_OceanTime*.09)).yz*.19+
        OceanNoiseGradient(p*2.7+float2(-_OceanTime*.13,_OceanTime*.17)).yz*.065);
    [loop]for(int i=0;i<_OceanActorCount;i++)
    {
        float2 d=p-_OceanActors[i].xy;float r=length(d)+.01;
        g+=d/r*cos(r*8-_OceanTime*4)*exp(-r/2)*_OceanActors[i].w*.05;
    }
    return normalize(float3(-g.x,1,-g.y));
}
float3 OceanSky(float3 direction)
{
    float3 d=normalize(direction);float y=saturate(d.y);
    float3 horizon=float3(.40,.64,.83),zenith=float3(.015,.25,.59);
    float3 day=lerp(horizon,zenith,pow(y,.42));
    float sun=saturate(dot(d,normalize(_OceanSunDirection.xyz)));
    day+=float3(1,.68,.35)*pow(sun,24)*.18;
    day+=float3(1,.91,.68)*smoothstep(.99991,.99998,sun)*3;
    float2 cloudUV=d.xz/max(.035,d.y)*1.3+float2(_OceanTime*.008,_OceanTime*.003);
    float cloud=OceanNoise(cloudUV)*.48+OceanNoise(cloudUV*2.03)*.28+OceanNoise(cloudUV*4.1)*.15+OceanNoise(cloudUV*8.3)*.09;
    cloud=smoothstep(lerp(.62,.48,_OceanWind),.78,cloud)*smoothstep(.08,.25,y);
    day=lerp(day,float3(.89,.91,.9),cloud*.72);
    float3 night=lerp(float3(.025,.05,.085),float3(.006,.015,.04),pow(y,.4));
    float2 starUV=float2(atan2(d.z,d.x)*.1591549,asin(clamp(d.y,-1,1))*.3183099)*float2(1800,900);
    float stars=step(.996,OceanHash(floor(starUV)))*pow(saturate(1-length(frac(starUV)-.5)*3),2)*smoothstep(.08,.2,y);
    float3 moonDir=normalize(float3(-_OceanSunDirection.x,max(.25,abs(_OceanSunDirection.y)),-_OceanSunDirection.z));
    night+=pow(saturate(dot(d,moonDir)),3000)*float3(.65,.75,.9)+stars*.35;
    return lerp(night,day,_OceanDaylight);
}
float OceanCaustic(float2 p)
{
    p+=float2(sin(p.y*.73+_OceanTime*.4),sin(p.x*.61-_OceanTime*.3))*.25;
    float2 cell=floor(p),f=frac(p);float first=8,second=8;
    [unroll]for(int y=-1;y<=1;y++)[unroll]for(int x=-1;x<=1;x++)
    {
        float2 g=float2(x,y);float2 h=float2(OceanHash(cell+g),OceanHash(cell+g+71.7));
        float2 at=g+.5+.33*sin(h*6.283+_OceanTime*float2(.51,.39))-f;float dist=dot(at,at);
        if(dist<first){second=first;first=dist;}else second=min(second,dist);
    }
    return 1-smoothstep(.015,.09,second-first);
}
float OceanWake(float2 p)
{
    float foam=0;
    [loop]for(int i=0;i<_OceanActorCount;i++)
    {
        float2 delta=p-_OceanActors[i].xy;float radius=_OceanActors[i].z;
        float2 velocity=_OceanVelocities[i].xy;float speed=length(velocity);
        float2 direction=velocity/max(speed,.01);
        float along=dot(delta,-direction),across=abs(delta.x*direction.y-delta.y*direction.x);
        float lengthFade=1-smoothstep(1,lerp(3,12,_OceanVelocities[i].z),along);
        float v=exp(-abs(across-along*.22)*3/max(radius,.2))*smoothstep(.2,1,along)*lengthFade;
        float ring=exp(-abs(length(delta)-radius)*5)*.2;
        foam+=max(v,ring)*_OceanActors[i].w;
    }
    return saturate(foam);
}
#endif
