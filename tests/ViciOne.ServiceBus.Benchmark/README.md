<!-- ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07. -->
Build an image (note that buildkit is mandatory):

DOCKER_BUILDKIT=1 docker build -f Dockerfile -t \<your-tag> ../../

To see the options:

docker run --rm --name mt-bench \<your-tag> -?
