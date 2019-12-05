--DROP SCHEMA "unit_tests" CASCADE;
CREATE SCHEMA "unit_tests";
GRANT ALL ON SCHEMA "unit_tests" TO "testRunner"; -- set a user name of that user who is connected to your db

--drop table "unit_tests"."entity_with_unique_columns";
create table "unit_tests"."entity_with_unique_columns"
(
	id serial not null
		constraint entity_with_unique_columns_pk primary key,
	record_id text,
	sensor_id text,
    value integer,
    constraint business_identity unique (record_id, sensor_id)
);

--drop table "unit_tests"."simple_test_entity";
create table "unit_tests"."simple_test_entity"
(
	id serial not null
		constraint simple_test_entity_pk primary key,
	record_id text,
	sensor_id text,
    value integer
);
