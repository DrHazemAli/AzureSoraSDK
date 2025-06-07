using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace AzureSoraSDK.Exceptions
{
    /// <summary>
    /// Base exception for all Sora SDK errors
    /// </summary>
    [Serializable]
    public class SoraException : Exception
    {
        /// <summary>
        /// Error code associated with this exception
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// HTTP status code if this error resulted from an HTTP request
        /// </summary>
        public HttpStatusCode? HttpStatusCode { get; set; }

        /// <summary>
        /// Request ID for tracking
        /// </summary>
        public string? RequestId { get; set; }

        public SoraException() : base() { }

        public SoraException(string message) : base(message) { }

        public SoraException(string message, Exception innerException) 
            : base(message, innerException) { }

        public SoraException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public SoraException(string message, HttpStatusCode httpStatusCode, string? errorCode = null) 
            : base(message)
        {
            HttpStatusCode = httpStatusCode;
            ErrorCode = errorCode;
        }

        protected SoraException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ErrorCode = info.GetString(nameof(ErrorCode));
            RequestId = info.GetString(nameof(RequestId));
            if (info.GetValue(nameof(HttpStatusCode), typeof(HttpStatusCode?)) is HttpStatusCode statusCode)
            {
                HttpStatusCode = statusCode;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(RequestId), RequestId);
            info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
        }
    }

    /// <summary>
    /// Exception thrown when authentication fails
    /// </summary>
    [Serializable]
    public class SoraAuthenticationException : SoraException
    {
        public SoraAuthenticationException(string message) 
            : base(message, HttpStatusCode.Unauthorized, "AUTH_FAILED") { }

        protected SoraAuthenticationException(SerializationInfo info, StreamingContext context) 
            : base(info, context) { }
    }

    /// <summary>
    /// Exception thrown when a requested resource is not found
    /// </summary>
    [Serializable]
    public class SoraNotFoundException : SoraException
    {
        public string? ResourceId { get; set; }

        public SoraNotFoundException(string message, string? resourceId = null) 
            : base(message, HttpStatusCode.NotFound, "NOT_FOUND")
        {
            ResourceId = resourceId;
        }

        protected SoraNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ResourceId = info.GetString(nameof(ResourceId));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ResourceId), ResourceId);
        }
    }

    /// <summary>
    /// Exception thrown when rate limits are exceeded
    /// </summary>
    [Serializable]
    public class SoraRateLimitException : SoraException
    {
        public TimeSpan? RetryAfter { get; set; }
        public int? RemainingRequests { get; set; }
        public DateTimeOffset? ResetTime { get; set; }

        public SoraRateLimitException(string message, TimeSpan? retryAfter = null) 
            : base(message, HttpStatusCode.TooManyRequests, "RATE_LIMIT_EXCEEDED")
        {
            RetryAfter = retryAfter;
        }

        protected SoraRateLimitException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            if (info.GetValue(nameof(RetryAfter), typeof(TimeSpan?)) is TimeSpan retryAfter)
                RetryAfter = retryAfter;
            RemainingRequests = info.GetInt32(nameof(RemainingRequests));
            if (info.GetValue(nameof(ResetTime), typeof(DateTimeOffset?)) is DateTimeOffset resetTime)
                ResetTime = resetTime;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(RetryAfter), RetryAfter);
            info.AddValue(nameof(RemainingRequests), RemainingRequests);
            info.AddValue(nameof(ResetTime), ResetTime);
        }
    }

    /// <summary>
    /// Exception thrown when a timeout occurs
    /// </summary>
    [Serializable]
    public class SoraTimeoutException : SoraException
    {
        public TimeSpan Timeout { get; set; }

        public SoraTimeoutException(string message, TimeSpan timeout) 
            : base(message, "TIMEOUT")
        {
            Timeout = timeout;
        }

        protected SoraTimeoutException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            Timeout = (TimeSpan)info.GetValue(nameof(Timeout), typeof(TimeSpan))!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Timeout), Timeout);
        }
    }

    /// <summary>
    /// Exception thrown when request validation fails
    /// </summary>
    [Serializable]
    public class SoraValidationException : SoraException
    {
        public Dictionary<string, string[]>? ValidationErrors { get; set; }

        public SoraValidationException(string message, Dictionary<string, string[]>? errors = null) 
            : base(message, HttpStatusCode.BadRequest, "VALIDATION_FAILED")
        {
            ValidationErrors = errors;
        }

        protected SoraValidationException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ValidationErrors = info.GetValue(nameof(ValidationErrors), typeof(Dictionary<string, string[]>)) 
                as Dictionary<string, string[]>;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ValidationErrors), ValidationErrors);
        }
    }
} 