namespace TouchSizeAvalonia.PointerConverters;

readonly
    record
    struct
    RawPointerPoint
    (
        int Id,
        double X,
        double Y,
        double PixelWidth,
        double PixelHeight,
        double PhysicalWidth,
        double PhysicalHeight,
        string Info
    );