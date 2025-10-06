namespace Unfold.BlockCompression;

public enum Bc7Field : byte
{
    Mode,

    Partition,
    Rotation,
    IndexSelection,

    Red0,
    Red1,
    Red2,
    Red3,
    Red4,
    Red5,
    RedAll,

    Green0,
    Green1,
    Green2,
    Green3,
    Green4,
    Green5,
    GreenAll,

    Blue0,
    Blue1,
    Blue2,
    Blue3,
    Blue4,
    Blue5,
    BlueAll,

    Alpha0,
    Alpha1,
    Alpha2,
    Alpha3,
    AlphaAll,

    EndpointP0,
    EndpointP1,
    EndpointP2,
    EndpointP3,
    EndpointP4,
    EndpointP5,
    EndpointPAll,

    SharedP0,
    SharedP1,
    SharedPAll,

    Indices,
    Indices2,
}
