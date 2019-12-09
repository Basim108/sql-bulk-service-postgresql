-- ------------- after update tests ----------------

--drop table "unit_tests"."after_update_tests";
create table "unit_tests"."after_update_tests"
(
    id serial not null
        constraint after_update_tests_pk primary key,
    record text,
    sensor text,
    value integer
);

--drop function after_update_tests_change_record_after_update;
create or replace function after_update_tests_change_record_after_update() returns trigger as $$
begin
    new."record" = 'new-value-changed-by-trigger';
    new."value" = new.value - 1;
    return new;
end
$$ language plpgsql;

--drop trigger "after_update_tests_change_record_trigger" on "unit_tests"."after_update_tests";
create trigger after_update_tests_change_record_trigger before update on "unit_tests"."after_update_tests"
    for each row execute procedure after_update_tests_change_record_after_update();