/*
 * A Helper Class that contains the UserMetadata used when cacheing players (PNUuidMetadataResult has internal set)
 */
using System;
using System.Collections.Generic;

public class UserMetadata
{
    public string Uuid { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string ExternalId { get; set; }

    public string ProfileUrl { get; set; }

    public Dictionary<string, object> Custom { get; set; }

    public string Updated { get; set; }
}