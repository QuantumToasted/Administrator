namespace Administrator.Core;

public interface IRemoteAttachment
{
    Guid Key { get; }
    
    string FileName { get; }
}