namespace Lapine.Protocol;

enum FrameType : Byte {
    Method = 0x01,
    Header = 0x02,
    Body = 0x03,
    Heartbeat = 0x08
}
