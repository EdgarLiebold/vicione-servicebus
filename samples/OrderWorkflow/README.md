# Order workflow sample

This compile-time sample shows public ServiceBus abstractions from a consumer's point of view: a
consumer and its definition, a correlated saga and its definition, a routing-slip activity and its
definition, message contracts, topology exclusion, publish, response, header, and timestamp usage.

The project is intentionally broker-independent and is never packed as a product package. Its
membership in ViciOne.ServiceBus.Engineering.slnx keeps every demonstrated public API shape under
the locked Release build.
