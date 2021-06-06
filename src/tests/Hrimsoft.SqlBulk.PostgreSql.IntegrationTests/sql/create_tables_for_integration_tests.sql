DROP SCHEMA IF EXISTS "unit_tests" CASCADE;
CREATE SCHEMA "unit_tests";

drop table if exists "unit_tests"."entity_with_dates";
create table "unit_tests"."entity_with_dates"
(
    id        serial not null
        constraint entity_with_dates_pk primary key,
    date      date   not null,
    dt        timestamp,
    dt_offset timestamptz
);

drop table if exists "unit_tests"."entity_with_unique_columns";
create table "unit_tests"."entity_with_unique_columns"
(
    id             serial not null
        constraint entity_with_unique_columns_pk primary key,
    record_id      text,
    sensor_id      text,
    value          integer,
    nullable_value integer,
    constraint business_identity unique (record_id, sensor_id)
);


drop table if exists "unit_tests"."entity_with_int_enum";
create table "unit_tests"."entity_with_int_enum"
(
    id              serial not null
        constraint entity_with_int_enum_pk primary key,
    some_enum_value integer
);

drop table if exists "unit_tests"."entity_with_string_enum";
create table "unit_tests"."entity_with_string_enum"
(
    id              serial not null
        constraint entity_with_string_enum_pk primary key,
    some_enum_value varchar
);

-- ------------- after update tests ----------------

drop table if exists "unit_tests"."after_update_tests";
create table "unit_tests"."after_update_tests"
(
    id     serial not null
        constraint after_update_tests_pk primary key,
    record text,
    sensor text,
    value  integer
);

drop trigger if exists "after_update_tests_change_record_trigger" on "unit_tests"."after_update_tests";
drop function if exists after_update_tests_change_record_after_update;

create or replace function after_update_tests_change_record_after_update() returns trigger as
$$
begin
    new."record" = 'new-value-changed-by-trigger';
    new."value" = new.value - 1;
return new;
end
$$ language plpgsql;

create trigger after_update_tests_change_record_trigger
    before update
    on "unit_tests"."after_update_tests"
    for each row
    execute procedure after_update_tests_change_record_after_update();

drop table if exists "unit_tests"."simple_test_entity";
create table "unit_tests"."simple_test_entity"
(
    id           serial  not null
        constraint simple_test_entity_pk primary key,
    record_id    text,
    sensor_id    text,
    value        integer not null,
    nullable_int integer
);

drop table if exists "unit_tests"."nullable_test_entity";
create table "unit_tests"."nullable_test_entity"
(
    id               serial not null
        constraint nullable_test_entity_pk primary key,
    nullable_int     integer,
    nullable_short   smallint,
    nullable_decimal numeric,
    nullable_float   real,
    nullable_double  double precision,
    nullable_bool    boolean
);

drop table if exists "unit_tests"."bulk_test_entity";
create table "unit_tests"."bulk_test_entity"
(
    id           serial  not null
        constraint bulk_test_entity_pk primary key,
    record_id    text,
    sensor_id    text,
    value        integer not null,
    nullable_int integer
);

drop table if exists "unit_tests"."entity_with_composite_pk";
create table "unit_tests"."entity_with_composite_pk"
(
    user_id integer not null,
    column2 text    not null,
    column3 integer not null,
    CONSTRAINT "PK_entity_with_composite_pk" PRIMARY KEY (user_id, column2)
);