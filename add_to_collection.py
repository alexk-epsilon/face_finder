import boto3
import os
from config import *
from pdb import set_trace as b



def add_faces_to_collection(bucket_name, collection_id):

    session = boto3.Session(profile_name='default')
    client = session.client('rekognition')
    s3=boto3.resource('s3')
    bucket = s3.Bucket(name="celeb-db")

    #list dirs in collection
    bad_noindex = 0
    bad_too_many = 0
    cnt = 0
    for obj in bucket.objects.filter():
        obj_path = obj.key
        name,ext = os.path.splitext(obj_path)
        if ext == ".jpg":
            dir,_ = os.path.split(name)
            
            cnt+=1

            external_id = dir
            print('id:'+external_id)

            if len(external_id) == 0:
                print("Invalid object:"+obj_path)
                continue
            
            photo = obj_path
            response = client.index_faces(CollectionId=collection_id,
                                    Image={'S3Object': {'Bucket': bucket_name, 'Name': photo}},
                                    ExternalImageId=external_id,
                                    MaxFaces=1,
                                    QualityFilter="AUTO",
                                    DetectionAttributes=['ALL'])
            
            num_faces = len(response['FaceRecords'])

            if(num_faces == 0):
                print("Photo has no usable faces:"+external_id)
                bad_noindex += 1
            
            if(num_faces > 1):
                print("Photo has too many faces:"+external_id)
                bad_too_many += 1

    print(f"Unusable:{bad_noindex}")
    print(f"Vague:{bad_too_many}")
    print(f"Total:{cnt}")

def main():
    bucket = BUCKET_NAME
    collection_id = BUCKET_NAME
    add_faces_to_collection(bucket, collection_id)


if __name__ == "__main__":
    main()