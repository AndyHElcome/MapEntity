namespace MMIv8_Ktype.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class GroupName(string name) : Attribute
    {
        public string Name { get; } = name;
    }
}
