# LV Stream Store #

## Project Focus ##

For event-based software, there are times where the volume of data is rather low, where only a few events are produced _per hour_ or _per-day_ with a limited potential of append collisions.

* The cost of using a server-based solution such as Sql Server (SqlStreamStore), Postgres (Marten), or ESDB (EventStore, Ltd) can cause the system to cost 3x as much just because of the storage of the events.
* After 5 years of running a low volume system full-speed over 100 users,  the total amount of generated data to be <20MB.

To solve this cost issue, LV Stream Store (the LV is for Low Volume, hence the name) is an in-process managed library that allows the storage and retrieval of stream information equivalent to the other aforementioned libraries.

## Current Supported Scenarios ##

### Persisters (Name needs evaluation) ###

The persisters are the layer between the data files and the start of mapping the emitted data into CLR objects.

We currently have (or will have) the following persisters available:

 * In-Memory
 * On-Disk
 * Azure Page Storage

This should cover the needs of:

* Testing, use the In-Memory persister.
* Local disk, use the on-disk persister.
* In Azure, use the Azure Page persister.

## Future Items ##

### Additional Serializers ###

Currently, the system is using System.Text.Json to store and retrieve data.  It would be advantageous to look at other serialization toolsets, which may provide more speed (although, for this too, speed is not necessarily a concern).

* Protobuf
* Newtonsoft.Json

### Additional "Persisters" ###

We will be looking for additional providers for:
 * AWS Storage (TBD what this is)
 * GCP Storage (TBD what this is)