﻿using StreamChat.Core.Helpers;
using StreamChat.Core.InternalDTO.Events;
using StreamChat.Core.InternalDTO.Models;
using StreamChat.Core.LowLevelClient.Models;

namespace StreamChat.Core.LowLevelClient.Events
{
    public partial class EventMessageUpdated : EventBase, ILoadableFrom<MessageUpdatedEventInternalDTO, EventMessageUpdated>
    {
        public string ChannelId { get; set; }

        public string ChannelType { get; set; }

        public string Cid { get; set; }

        public System.DateTimeOffset? CreatedAt { get; set; }

        public Message Message { get; set; }

        public string Team { get; set; }

        public System.Collections.Generic.List<User> ThreadParticipants { get; set; }

        public string Type { get; set; }

        public User User { get; set; }

        EventMessageUpdated ILoadableFrom<MessageUpdatedEventInternalDTO, EventMessageUpdated>.LoadFromDto(MessageUpdatedEventInternalDTO dto)
        {
            ChannelId = dto.ChannelId;
            ChannelType = dto.ChannelType;
            Cid = dto.Cid;
            CreatedAt = dto.CreatedAt;
            Message = Message.TryLoadFromDto<MessageInternalDTO, Message>(dto.Message);
            Team = dto.Team;
            ThreadParticipants = ThreadParticipants.TryLoadFromDtoCollection(dto.ThreadParticipants);
            Type = dto.Type;
            User = User.TryLoadFromDto<UserObjectInternalDTO, User>(dto.User);
            AdditionalProperties = dto.AdditionalProperties;

            return this;
        }
    }
}