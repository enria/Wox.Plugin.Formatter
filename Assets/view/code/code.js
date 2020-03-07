var inputLan='sql',textData=`select *
from (
    select*
    from stu
    where grade = 7) s
left join (
    select*
    from sco
    where subject = "math") t
on s.id = t.stu_id;`