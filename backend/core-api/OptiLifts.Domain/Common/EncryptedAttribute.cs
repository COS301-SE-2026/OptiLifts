namespace OptiLifts.Domain.Common;

//this allows us to mark attributes to be encryypted in the db easily
[AttributeUsage(AttributeTargets.Property)]
public class EncryptedAttribute : Attribute { }