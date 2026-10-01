# Amazon S3 message data

The optional `ViciOne.ServiceBus.AmazonS3` adapter stores message payloads in a caller-owned general-purpose bucket. Supply an `IAmazonS3` client and `AmazonS3MessageDataRepositoryOptions` through `UseAmazonS3`. The caller retains ownership of the client and of every stream passed to `PutAsync`. A directly constructed repository checks bucket readiness on its first upload.

`PutAsync` uploads from the stream's current position. A null `timeToLive` leaves the object outside the repository-owned expiration rule. An explicit TTL must be a positive whole number of days equal to `LifecycleExpirationDays`; the upload receives the tag `vicione-servicebus-message-data-expiration=enabled`, and the repository-owned lifecycle rule expires only objects with that tag. Other lifecycle rules in the caller-owned bucket remain under the bucket owner's control. For example, a foreign all-object expiration rule can still delete a null-TTL object; use a bucket without overlapping rules when unbounded storage is required.

## AWS permissions

- Every startup reads the bucket lifecycle configuration to detect an older product-owned rule that would expire untagged uploads. Grant `s3:GetLifecycleConfiguration` even when `LifecycleExpirationDays` is null. Startup fails if this inspection is denied.
- If `LifecycleExpirationDays` is configured, startup may create the bucket and update its product-owned lifecycle rule. Grant the corresponding bucket creation and lifecycle-write permissions for those operations.
- With configured expiration, startup and explicit-TTL uploads read bucket versioning and require `s3:GetBucketVersioning`. Buckets whose versioning is enabled or suspended are rejected: S3's ordinary expiration action leaves the payload in a noncurrent version. Keep versioning disabled for the bucket's lifetime; a change between inspection and upload remains an external configuration race.
- An upload with explicit TTL includes an object tag and needs `s3:PutObjectTagging` in addition to the usual object-write permission. A null-TTL upload does not request this tag.

AWS documents the permissions for [reading lifecycle configuration](https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html), [reading versioning](https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketVersioning.html), and [uploading an object with tags](https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutObject.html). AWS also explains why [expiration leaves noncurrent versions behind](https://docs.aws.amazon.com/AmazonS3/latest/userguide/DeletingObjectVersions.html). LocalStack tests verify request behavior; real AWS IAM and lifecycle deletion still need an external provider run.

## Bucket readiness

Startup rejects an existing product-owned expiration rule unless its filter selects only objects
with the required expiration tag. Select a bucket without a conflicting rule for a new deployment.
The repository does not provide an automatic object migration or silently replace a rule that could
affect existing objects.
