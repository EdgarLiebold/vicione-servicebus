#
# ActiveMQ Classic test fixture, base image bound by digest.
#
# The only delta against the official configuration is schedulerSupport="true"; the ActiveMQ
# default is false and DelayRetry_Specs requires scheduled delivery. Jolokia is already served
# by the bundled web console on 8161 and needs no change. Credentials are supplied at container
# start through ACTIVEMQ_CONNECTION_* and ACTIVEMQ_WEB_*, never baked into a layer.
FROM apache/activemq-classic:6.2.0@sha256:992bc29a8459f6772ff14d168a8b011c9cde7a769ec01d72788c9cc4e9229564

COPY activemq.xml /opt/apache-activemq/conf/activemq.xml

# The official entrypoint writes ACTIVEMQ_WEB_USER into users.properties but leaves
# groups.properties at the stock "admins=admin". The web console and Jolokia authorise by role,
# so the run account would authenticate and then be refused with HTTP 403. Binding the fixed
# account name to the admins role closes that gap. Only the name is fixed here; the password is
# generated per run and never stored.
COPY groups.properties /opt/apache-activemq/conf/groups.properties
