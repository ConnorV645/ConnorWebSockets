using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConnorWebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ConnorWebSockets.Bases
{
    public abstract class SocketCacheBase<T, Y> where T : IWebSocket
    {
        protected readonly IConnectionMultiplexer connectionMultiplexer;
        protected readonly ILogger<SocketCacheBase<T, Y>> logger;

        public static readonly ConcurrentDictionary<Y, List<T>> socketList = new ConcurrentDictionary<Y, List<T>>();

        public SocketCacheBase(IConnectionMultiplexer connectionMultiplexer, ILogger<SocketCacheBase<T, Y>> logger)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            this.logger = logger;
        }

        /// <summary>
        /// Subscribe a socket to a channel
        /// </summary>
        /// <param name="socketHandler"></param>
        /// <param name="socket"></param>
        /// <param name="keyId"></param>
        /// <param name="receiveAction">(Receive Value, This Socket, List of Sockets) should be async</param>
        /// <returns></returns>
        public virtual async Task SubscribeToChannel(T socket, Y keyId, Func<string, T, List<T>, Task> receiveAction)
        {
            if (socketList.ContainsKey(keyId) && socketList.TryGetValue(keyId, out var list))
            {
                list.Add(socket);
                if (!socketList.ContainsKey(keyId))
                {
                    // key was removed while we were adding to it - need to retry
                    await SubscribeToChannel(socket, keyId, receiveAction);
                }
            }
            else
            {
                if (socketList.TryAdd(keyId, new List<T>() { socket }))
                {
                    await connectionMultiplexer.GetSubscriber().SubscribeAsync(GetFullKey(keyId), async (channel, value) =>
                    {
                        if (SocketCacheBase<T, Y>.socketList.TryGetValue(GetIdFromFullKey(channel), out var socketList))
                        {
                            try
                            {
                                await receiveAction(value, socket, socketList);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error Receiving Message From Redis");
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Unsubscribe socket from channel
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public virtual async Task UnsubscribeFromChannel(T socket, Y keyId)
        {
            if (socketList.ContainsKey(keyId) && socketList.TryGetValue(keyId, out var list))
            {
                list.Remove(socket);
                if (list.Count == 0)
                {
                    await connectionMultiplexer.GetSubscriber().UnsubscribeAsync(GetFullKey(keyId));
                    socketList.TryRemove(keyId, out _);
                }
            }
        }

        /// <summary>
        /// Unsubscribe socket from many channels
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="keyIds"></param>
        /// <returns></returns>
        public virtual async Task UnsubscribeFromMany(T socket, List<Y> keyIds)
        {
            foreach (var keyId in keyIds)
            {
                await UnsubscribeFromChannel(socket, keyId);
            }
        }

        /// <summary>
        /// Publish to all sockets for a key
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual async Task PublishMessage(Y keyId, string value)
        {
            await connectionMultiplexer.GetSubscriber().PublishAsync(GetFullKey(keyId), value);
        }

        /// <summary>
        /// Publish to all sockets for multiple keys
        /// </summary>
        /// <param name="keyIds"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual async Task PublishMessagesToMultiple(List<Y> keyIds, string value)
        {
            foreach (var keyId in keyIds)
            {
                await PublishMessage(keyId, value);
            }
        }

        /// <summary>
        /// This will be the key stored in redis
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        protected abstract string GetFullKey(Y keyId);

        /// <summary>
        /// This is the Id returned from the full key stored in redis
        /// </summary>
        /// <param name="fullKey"></param>
        /// <returns></returns>
        protected abstract Y GetIdFromFullKey(string fullKey);
    }
}
