import psycopg2 
import os
import pandas as pd
from pdb import set_trace as b
import json
import base64
import threading

fields_query = "SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_name = 'zzz_export_ud_w_passport';"
conn = psycopg2.connect("dbname=police user=postgres")
cur = conn.cursor()
cur.execute(fields_query)
tl = cur.fetchall()
fields = [f[1] for f in tl]

select_query = "SELECT * from zzz_export_ud_w_passport;"
#select_query = "SELECT * from zzz_export_ud_w_passport where identif='3010154M086PB0'"
cur.execute(select_query)
tuples = cur.fetchall()
df = pd.DataFrame(tuples, columns=fields)

base_dir = "/mnt/f/projects/pokemongo_aws/police_db"
not_decodable = 0
lock = threading.Lock()


def process_frame(row):
    
    #get id
    id = row.identif
    #make directory
    record_dir = os.path.join(base_dir,id)

    os.makedirs(record_dir,exist_ok=True)
    #decode base64 and create jpg
    try:
        bin_data = base64.b64decode(row.image)
    except:
        row['image']='Could not be decoded'
        lock.acquire()
        not_decodable += 1
        lock.release()
        pass
    else:
        row['image']='Present'
        pass

    with open(os.path.join(record_dir,"original.jpg"),'wb') as f:
        f.write(bin_data)

    #aggregate all non-null fields and create json
    json_object = json.dumps([row.dropna().to_dict()],indent=4,sort_keys=True, default=str,ensure_ascii=False)
    with open(os.path.join(record_dir,"personal_data.json"), "w") as j:
        j.write(json_object)
    
    print("Done:"+id)
    pass

df.apply(process_frame,axis=1)

print(f"Unable to decode:{not_decodable} images")